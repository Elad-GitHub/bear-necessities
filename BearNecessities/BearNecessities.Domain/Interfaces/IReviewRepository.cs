using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Interfaces;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Review>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Review review, CancellationToken cancellationToken = default);
    Task UpdateAsync(Review review, CancellationToken cancellationToken = default);
    Task<List<Review>> GetByTargetAsync(string targetType, Guid targetId, int pageSize, Guid? cursor, CancellationToken cancellationToken = default);
    Task<int> CountRevealedAsync(string targetType, Guid targetId, CancellationToken cancellationToken = default);
    Task<decimal?> GetAverageRatingAsync(string targetType, Guid targetId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(UserId authorId, string targetType, Guid targetId, CancellationToken cancellationToken = default);
    Task<List<Review>> GetByAuthorAsync(UserId authorId, int pageSize, Guid? cursor, CancellationToken cancellationToken = default);
}
