using BearNecessities.Domain.Enums;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Interfaces;

public interface ICurrentUserService
{
    UserId? UserId { get; }
    bool IsAuthenticated { get; }
    bool HasRole(UserRole role);
}
