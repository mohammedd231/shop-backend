using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopApplication.Domain.Entities;
using ShopApplication.Infrastructure.Persistence;

namespace ShopApplication.Api.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize] // user must be logged in
public class CartController : ControllerBase
{
    private readonly AppDbContext _db;
    public CartController(AppDbContext db) => _db = db;

    // DTOs
    public record AddItemDto(Guid ProductId, int Quantity);

    // ===== Helpers =====

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var v = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
            ?? user.FindFirstValue("sub");
        return Guid.TryParse(v, out var id) ? id : Guid.Empty;
    }

    private static bool IsUniqueCartLineConflict(DbUpdateException ex)
    {
        var msg = (ex.InnerException?.Message ?? ex.Message) ?? string.Empty;
        return msg.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("UNIQUE KEY", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
    }

    // ===== Endpoints =====

    // GET /api/cart
[HttpGet]
public async Task<IActionResult> GetMyCart()
{
    var userId = GetUserId(User);
    if (userId == Guid.Empty) return Unauthorized();

    var cart = await _db.Carts
        .Include(c => c.Items)
        .FirstOrDefaultAsync(c => c.UserId == userId);

    // create cart if missing
    if (cart is null)
    {
        cart = new Cart(userId);
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync();
    }

    // Enrich with current product data (imageUrl/category) WITHOUT changing snapshots
    var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
    var products = await _db.Products
        .Where(p => productIds.Contains(p.Id))
        .ToDictionaryAsync(p => p.Id, p => new { p.ImageUrl, p.Category, p.Name });

    var items = cart.Items.Select(i =>
    {
        products.TryGetValue(i.ProductId, out var prod);
        return new
        {
            id = i.ProductId,                 // handy alias for UI
            productId = i.ProductId,
            name = i.Name ?? prod?.Name ?? "Unknown product",
            price = i.Price,
            quantity = i.Quantity,
            lineTotal = i.LineTotal,
            imageUrl = prod?.ImageUrl,        // NEW: pass image
            category = prod?.Category         // optional extra
        };
    });

    return Ok(new
    {
        userId,
        items,
        total = cart.Total
    });
}


    // POST /api/cart/items
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddItemDto body)
    {
        if (body is null || body.ProductId == Guid.Empty || body.Quantity <= 0)
            return BadRequest("Invalid product or quantity.");

        var userId = GetUserId(User);
        if (userId == Guid.Empty) return Unauthorized();

        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == body.ProductId);
        if (product is null) return NotFound("Product not found.");

        // ---- Phase 1: ensure Cart row exists & is persisted (separate save) ----
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null)
        {
            cart = new Cart(userId);
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync(); // persist Cart first
        }

        // ---- Phase 2: add/increase item ----
        if (_db.Database.IsSqlite())
        {
            // Use a single upsert statement to avoid any EF race:
            // Assumes your table is 'CartItems' with columns:
            //   Id (GUID), CartId (GUID), ProductId (GUID), Name (TEXT), Price (DECIMAL), Quantity (INT)
            var newLineId = Guid.NewGuid();

            // Use interpolated SQL to avoid manual parameters
            var rows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO CartItems (Id, CartId, ProductId, Name, Price, Quantity)
VALUES ({newLineId}, {cart.Id}, {product.Id}, {product.Name}, {product.Price}, {body.Quantity})
ON CONFLICT(CartId, ProductId) DO UPDATE SET
    Quantity = Quantity + {body.Quantity},
    Name = excluded.Name,
    Price = excluded.Price;");

            // rows is 1 for insert, or 1 for update in SQLite
            return Ok(new { message = "Added", mode = rows == 1 ? "upsert" : "upsert" });
        }
        else
        {
            // Fallback for non-SQLite providers: robust EF retry
            const int maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    // Work on a fresh tracked instance incl. items
                    cart = await _db.Carts
                        .Include(c => c.Items)
                        .FirstOrDefaultAsync(c => c.UserId == userId)
                        ?? new Cart(userId);

                    if (_db.Entry(cart).State == EntityState.Detached)
                        _db.Carts.Add(cart);

                    var existing = cart.Items.FirstOrDefault(i => i.ProductId == product.Id);
                    if (existing is null)
                    {
                        cart.AddItem(product.Id, product.Name, product.Price, body.Quantity);
                    }
                    else
                    {
                        cart.SetQuantity(product.Id, existing.Quantity + body.Quantity);
                    }

                    await _db.SaveChangesAsync();
                    return Ok(new { message = "Added", attempt });
                }
                catch (DbUpdateConcurrencyException)
                {
                    _db.ChangeTracker.Clear(); // stale tracked entities; reload & retry
                    if (attempt == maxAttempts)
                        return StatusCode(409, new { error = "cart_concurrency", message = "Cart was modified concurrently. Please retry." });
                }
                catch (DbUpdateException ex) when (IsUniqueCartLineConflict(ex))
                {
                    _db.ChangeTracker.Clear();
                    if (attempt == maxAttempts)
                        return StatusCode(409, new { error = "cart_concurrency", message = "Cart line collided. Please retry." });
                }
            }

            return StatusCode(500, "Unexpected failure.");
        }
    }

    // DELETE /api/cart/items/{productId}
    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid productId)
    {
        var userId = GetUserId(User);
        if (userId == Guid.Empty) return Unauthorized();

        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null) return NotFound("Cart not found.");

        cart.RemoveItem(productId);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // POST /api/cart/clear
    [HttpPost("clear")]
    public async Task<IActionResult> Clear()
    {
        var userId = GetUserId(User);
        if (userId == Guid.Empty) return Unauthorized();

        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null) return NotFound("Cart not found.");

        foreach (var i in cart.Items.ToList())
            cart.RemoveItem(i.ProductId);

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
