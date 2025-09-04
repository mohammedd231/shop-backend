using System.Security.Claims;

namespace ShopApplication.Api.Identity;

public static class CurrentUser
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier) 
                 ?? user.FindFirstValue("sub");
        return Guid.TryParse(id, out var gid) ? gid : Guid.Empty;
    }
}
