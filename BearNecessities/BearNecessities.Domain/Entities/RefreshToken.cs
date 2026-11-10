using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Entities;

public sealed class RefreshToken
{
    public const int DefaultExpiryDays = 7;

    private RefreshToken() { }

    public Guid Id { get; private set; }
    public UserId UserId { get; private set; } = default!;
    public string Token { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;

    public static RefreshToken Create(UserId userId, string token, DateTime expiresAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

    public void Revoke() => RevokedAt = DateTime.UtcNow;
}
