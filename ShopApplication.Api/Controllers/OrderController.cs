using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopApplication.Api.Identity;
using ShopApplication.Domain.Entities;
using ShopApplication.Infrastructure.Persistence;

namespace ShopApplication.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    public OrdersController(AppDbContext db) => _db = db;

    public record CheckoutDto(string? PaymentMethod); // placeholder

    // POST /api/orders/checkout  (customer)
    [Authorize]
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutDto? _)
    {
        var userId = CurrentUser.GetUserId(User);
        if (userId == Guid.Empty) return Unauthorized();

        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null || !cart.Items.Any())
            return BadRequest("Cart is empty.");

        var lines = cart.Items.Select(i => (i.ProductId, i.Name, i.Price, i.Quantity));
        var order = new Order(userId, lines);
        _db.Orders.Add(order);

        foreach (var it in cart.Items.ToList())
            cart.RemoveItem(it.ProductId);

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, new {
            order.Id,
            order.Status,
            order.Total
        });
    }

    // GET /api/orders/my  (customer)
    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> MyOrders()
    {
        var userId = CurrentUser.GetUserId(User);
        if (userId == Guid.Empty) return Unauthorized();

        var list = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync();

        return Ok(list.Select(o => new {
            o.Id, o.Status, o.Total, o.CreatedAtUtc,
            items = o.Items.Select(i => new { i.ProductId, i.Name, i.Price, i.Quantity })
        }));
    }

    // GET /api/orders/{id}  (customer sees own; admin sees any)
    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = CurrentUser.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();

        if (!isAdmin && order.UserId != userId) return Forbid();

        return Ok(order);
    }

    // GET /api/orders  (admin only)
    [Authorize(Policy = "AdminOnly")]
    [HttpGet]
    public async Task<IActionResult> All()
    {
        var list = await _db.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync();

        return Ok(list.Select(o => new { o.Id, o.UserId, o.Status, o.Total, o.CreatedAtUtc }));
    }

    // PATCH /api/orders/{id}/status  (admin only)
    public record UpdateStatusDto(string Status);

    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusDto body)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();

        var allowed = new[] { "Pending", "Paid", "Shipped", "Cancelled" };
        if (!allowed.Contains(body.Status)) return BadRequest("Invalid status.");

        if (body.Status == "Paid") order.MarkPaid();
        else
        {
            typeof(Order).GetProperty("Status")!.SetValue(order, body.Status);
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
