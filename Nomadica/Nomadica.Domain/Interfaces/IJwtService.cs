using System.Security.Claims;
using Nomadica.Domain.Entities;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    RefreshToken CreateRefreshToken(UserId userId);
    ClaimsPrincipal? ValidateToken(string token);
}
