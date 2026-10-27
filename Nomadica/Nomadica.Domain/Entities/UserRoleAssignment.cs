using Nomadica.Domain.Enums;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Entities;

public sealed class UserRoleAssignment
{
    private UserRoleAssignment() { }

    public UserId UserId { get; private set; } = default!;
    public UserRole RoleName { get; private set; }
    public string? Region { get; private set; }
    public DateTime AssignedAt { get; private set; }

    public static UserRoleAssignment Create(UserId userId, UserRole roleName, string? region = null) =>
        new()
        {
            UserId = userId,
            RoleName = roleName,
            Region = region,
            AssignedAt = DateTime.UtcNow
        };
}
