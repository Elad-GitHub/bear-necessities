using BearNecessities.Domain.Entities;

namespace BearNecessities.Domain.Interfaces;

public interface IEditSuggestionRepository
{
    Task AddAsync(EditSuggestion suggestion, CancellationToken cancellationToken = default);
    Task<EditSuggestion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<EditSuggestion>> GetPendingAsync(CancellationToken cancellationToken = default);
}
