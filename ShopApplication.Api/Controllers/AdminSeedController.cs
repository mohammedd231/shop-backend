using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopApplication.Infrastructure.Persistence;
using ShopApplication.Domain.Entities;

namespace ShopApplication.Api.Controllers;

[ApiController]
[Route("api/admin-seed")]
public class AdminSeedController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser<Guid>> _users;
    private readonly RoleManager<IdentityRole<Guid>> _roles;

    public AdminSeedController(AppDbContext db,
        UserManager<IdentityUser<Guid>> users,
        RoleManager<IdentityRole<Guid>> roles)
    {
        _db = db;
        _users = users;
        _roles = roles;
    }

    // 🟢 Anyone logged in can call this once to become Admin
    [Authorize]
    [HttpPost("make-me-admin")]
    public async Task<IActionResult> MakeMeAdmin([FromBody] string email)
    {
        var user = await _users.FindByEmailAsync(email);
        if (user is null) return NotFound("User not found.");

        // ensure role exists
        var roleName = "Admin";
        if (!await _roles.RoleExistsAsync(roleName))
            await _roles.CreateAsync(new IdentityRole<Guid>(roleName));

        var result = await _users.AddToRoleAsync(user, roleName);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok(new { email, addedRole = roleName });
    }

    // 🟠 Admin-only endpoint: import mock products
    [Authorize(Policy = "AdminOnly")]
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] List<ImportProductDto> items)
    {
        if (items is null || items.Count == 0) return BadRequest("Empty list.");

        var existing = _db.Products.ToList();
        var existingNames = existing
            .Select(p => p.Name.ToLowerInvariant())
            .ToHashSet();

        int created = 0, skipped = 0;

        foreach (var m in items)
        {
            if (string.IsNullOrWhiteSpace(m.name)) { skipped++; continue; }

            var key = m.name.ToLowerInvariant();
            if (existingNames.Contains(key)) { skipped++; continue; }

            var p = new Product(
                m.name,
                m.description ?? "",
                string.IsNullOrWhiteSpace(m.category) ? "general" : m.category,
                m.price,
                m.stockQuantity,
                m.imageUrl
            );

            _db.Products.Add(p);
            existingNames.Add(key);
            created++;
        }

        await _db.SaveChangesAsync();
        return Ok(new { created, skipped, total = items.Count });
    }

    public record ImportProductDto(
        string name,
        string? description,
        string category,
        decimal price,
        int stockQuantity,
        string? imageUrl
    );
}
