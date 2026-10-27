using System.Security.Claims;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Enums;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.API.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public UserId? UserId
    {
        get
        {
            var sub = User?.FindFirstValue("sub");
            return sub is not null && Guid.TryParse(sub, out var guid)
                ? new UserId(guid)
                : null;
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool HasRole(UserRole role)
    {
        var roles = User?.FindFirstValue("roles");
        if (roles is null) return false;
        return roles.Split(',').Any(r => string.Equals(r.Trim(), role.ToString(), StringComparison.OrdinalIgnoreCase));
    }
}
