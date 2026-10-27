using Microsoft.EntityFrameworkCore;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;
using Nomadica.Infrastructure.Persistence;

namespace Nomadica.Infrastructure.Repositories;

public sealed class ReviewRepository : IReviewRepository
{
    private readonly NomadiaDbContext _context;

    public ReviewRepository(NomadiaDbContext context)
    {
        _context = context;
    }

    public async Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Reviews.FindAsync(new object?[] { id }, cancellationToken);

    public async Task AddAsync(Review review, CancellationToken cancellationToken = default)
        => await _context.Reviews.AddAsync(review, cancellationToken);

    public Task UpdateAsync(Review review, CancellationToken cancellationToken = default)
    {
        _context.Reviews.Update(review);
        return Task.CompletedTask;
    }

    public async Task<List<Review>> GetByTargetAsync(string targetType, Guid targetId, int pageSize, Guid? cursor, CancellationToken cancellationToken = default)
    {
        var all = await _context.Reviews
            .Where(r => r.TargetType == targetType && r.TargetId == targetId && r.Status == ReviewStatus.Revealed)
            .OrderByDescending(r => r.RevealedAt)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var startIndex = 0;
        if (cursor.HasValue)
        {
            var idx = all.FindIndex(r => r.Id == cursor.Value);
            startIndex = idx >= 0 ? idx + 1 : 0;
        }

        return all.Skip(startIndex).Take(pageSize).ToList();
    }

    public async Task<int> CountRevealedAsync(string targetType, Guid targetId, CancellationToken cancellationToken = default)
        => await _context.Reviews.CountAsync(
            r => r.TargetType == targetType && r.TargetId == targetId && r.Status == ReviewStatus.Revealed,
            cancellationToken);

    public async Task<decimal?> GetAverageRatingAsync(string targetType, Guid targetId, CancellationToken cancellationToken = default)
    {
        var hasReviews = await _context.Reviews.AnyAsync(
            r => r.TargetType == targetType && r.TargetId == targetId && r.Status == ReviewStatus.Revealed,
            cancellationToken);

        if (!hasReviews) return null;

        return (decimal?)await _context.Reviews
            .Where(r => r.TargetType == targetType && r.TargetId == targetId && r.Status == ReviewStatus.Revealed)
            .AverageAsync(r => (double)r.Rating, cancellationToken);
    }

    public async Task<bool> ExistsAsync(UserId authorId, string targetType, Guid targetId, CancellationToken cancellationToken = default)
        => await _context.Reviews.AnyAsync(
            r => r.AuthorId == authorId && r.TargetType == targetType && r.TargetId == targetId,
            cancellationToken);

    public async Task<List<Review>> GetByAuthorAsync(UserId authorId, int pageSize, Guid? cursor, CancellationToken cancellationToken = default)
    {
        var all = await _context.Reviews
            .Where(r => r.AuthorId == authorId && r.Status != ReviewStatus.Removed)
            .OrderByDescending(r => r.CreatedAt)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var startIndex = 0;
        if (cursor.HasValue)
        {
            var idx = all.FindIndex(r => r.Id == cursor.Value);
            startIndex = idx >= 0 ? idx + 1 : 0;
        }

        return all.Skip(startIndex).Take(pageSize).ToList();
    }
}
