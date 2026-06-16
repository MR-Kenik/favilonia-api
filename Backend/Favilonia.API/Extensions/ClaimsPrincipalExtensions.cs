using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Favilonia.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetOrganizationId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("organizationId")?.Value;
        return Guid.TryParse(claim, out var organizationId) ? organizationId : null;
    }

    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
