using System.Security.Claims;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    RefreshToken CreateRefreshToken(UserId userId);
    ClaimsPrincipal? ValidateToken(string token);
}
