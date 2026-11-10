using BearNecessities.Domain.Enums;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Entities;

public sealed class UserSocialLogin : BaseEntity<Guid>
{
    private UserSocialLogin() { }

    public UserId UserId { get; private set; } = default!;
    public ExternalLoginProvider Provider { get; private set; }
    public string ProviderId { get; private set; } = default!;

    public static UserSocialLogin Create(UserId userId, ExternalLoginProvider provider, string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID cannot be empty.", nameof(providerId));

        return new UserSocialLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            ProviderId = providerId
        };
    }
}
