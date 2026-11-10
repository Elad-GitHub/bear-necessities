using Microsoft.EntityFrameworkCore;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;
using BearNecessities.Infrastructure.Persistence;

namespace BearNecessities.Infrastructure.Repositories;

public sealed class UserRepository : BaseRepository<User, UserId>, IUserRepository
{
    public UserRepository(BearNecessitiesDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await Context.Users
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant(), cancellationToken);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await Context.Users
            .AnyAsync(u => u.Email == email.Trim().ToLowerInvariant(), cancellationToken);

    public async Task AddRoleAssignmentAsync(UserRoleAssignment assignment, CancellationToken cancellationToken = default) =>
        await Context.UserRoleAssignments.AddAsync(assignment, cancellationToken);

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) =>
        await Context.RefreshTokens.AddAsync(refreshToken, cancellationToken);

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default) =>
        await Context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == token, cancellationToken);

    public async Task RevokeRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var refreshToken = await Context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == token, cancellationToken);
        refreshToken?.Revoke();
    }

    public async Task RevokeAllRefreshTokensForUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var activeTokens = await Context.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null && r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
            token.Revoke();
    }

    public async Task<List<User>> GetByIdsAsync(IEnumerable<UserId> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await Context.Users
            .Where(u => idList.Contains(u.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<User>> GetPendingMembershipAsync(CancellationToken cancellationToken = default)
        => await Context.Users
            .Where(u => u.MembershipStatus == Domain.Enums.MembershipStatus.Pending)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(cancellationToken);
}
