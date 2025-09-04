using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ShopApplication.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser<Guid>> _users;
    private readonly IConfiguration _cfg;

    public AuthController(UserManager<IdentityUser<Guid>> users, IConfiguration cfg)
    { _users = users; _cfg = cfg; }

    public record RegisterReq(string Email, string Password);
    public record LoginReq(string Email, string Password);

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterReq req)
    {
        var user = new IdentityUser<Guid> { UserName = req.Email, Email = req.Email };
        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);

        // New users start with no role; you can assign "Customer" here if you want:
        // await _users.AddToRoleAsync(user, "Customer");

        var roles = (await _users.GetRolesAsync(user)).ToList();
        return Ok(new { token = CreateJwt(user, roles) });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginReq req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user is null) return Unauthorized();

        var ok = await _users.CheckPasswordAsync(user, req.Password);
        if (!ok) return Unauthorized();

        var roles = (await _users.GetRolesAsync(user)).ToList();
        return Ok(new { token = CreateJwt(user, roles) });
    }

    private string CreateJwt(IdentityUser<Guid> user, IList<string> roles)
    {
        var issuer   = _cfg["Jwt:Issuer"]   ?? "shop-app";
        var audience = _cfg["Jwt:Audience"] ?? "shop-app";
        var secret   = _cfg["Jwt:Secret"]   ?? "TEMP_DEV_SECRET_change_me_64chars";

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        // 1) Standard ASP.NET role claim (supports [Authorize(Roles="Admin")])
        // 2) Plain 'role' claim for JS UIs (one per role)
        foreach (var r in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, r)); // standard
            claims.Add(new Claim("role", r));          // plain string
        }

        // 3) 'roles' = JSON array so frontends can parse an array if they want
        claims.Add(new Claim("roles", JsonSerializer.Serialize(roles)));

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
