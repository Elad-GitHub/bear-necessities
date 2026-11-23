using BearNecessities.Domain.Entities;

namespace BearNecessities.Domain.Interfaces;

public interface IFeedbackRepository
{
    Task AddAsync(FeedbackMessage feedback, CancellationToken cancellationToken = default);
    Task<FeedbackMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Messages not yet handled, newest first.</summary>
    Task<List<FeedbackMessage>> GetOpenAsync(CancellationToken cancellationToken = default);
}
