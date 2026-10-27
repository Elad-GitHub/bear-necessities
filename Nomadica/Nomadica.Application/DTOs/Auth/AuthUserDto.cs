namespace Nomadica.Application.DTOs.Auth;

public record AuthUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    List<string> Roles,
    string SubscriptionPlan,
    bool IsActive);
