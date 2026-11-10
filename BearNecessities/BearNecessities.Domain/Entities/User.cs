using BearNecessities.Domain.Enums;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Entities;

public sealed class User : BaseEntity<UserId>
{
    private readonly List<UserRole> _roles = new();
    private readonly List<UserSocialLogin> _socialLogins = new();

    private User() { }

    public string Email { get; private set; } = default!;
    public string? PasswordHash { get; private set; }
    public string DisplayName { get; private set; } = default!;
    public string? AvatarUrl { get; private set; }
    public string? Bio { get; private set; }
    public bool PhoneVerified { get; private set; }
    public bool IdVerified { get; private set; }
    public decimal TrustScore { get; private set; }
    public SubscriptionPlan SubscriptionPlan { get; private set; }
    public string PreferredLocale { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime? LastActiveAt { get; private set; }
    public MembershipStatus MembershipStatus { get; private set; }
    public string? SocialProfileUrl { get; private set; }

    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<UserSocialLogin> SocialLogins => _socialLogins.AsReadOnly();

    public static User Create(string email, string displayName)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty.", nameof(displayName));

        var user = new User
        {
            Id = new UserId(Guid.NewGuid()),
            Email = email.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            SubscriptionPlan = SubscriptionPlan.Free,
            TrustScore = 0.00m,
            IsActive = true,
            PreferredLocale = "en",
            PhoneVerified = false,
            IdVerified = false,
            MembershipStatus = MembershipStatus.Pending
        };
        user._roles.Add(UserRole.Traveler);
        return user;
    }

    public void SetPasswordHash(string hash) => PasswordHash = hash;

    public void SetSocialProfileUrl(string url) => SocialProfileUrl = url;

    public void ApproveMembership()
    {
        MembershipStatus = MembershipStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RejectMembership()
    {
        MembershipStatus = MembershipStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddRole(UserRole role)
    {
        if (!_roles.Contains(role))
            _roles.Add(role);
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
