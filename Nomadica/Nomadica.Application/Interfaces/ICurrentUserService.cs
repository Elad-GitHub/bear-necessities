using Nomadica.Domain.Enums;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.Interfaces;

public interface ICurrentUserService
{
    UserId? UserId { get; }
    bool IsAuthenticated { get; }
    bool HasRole(UserRole role);
}
