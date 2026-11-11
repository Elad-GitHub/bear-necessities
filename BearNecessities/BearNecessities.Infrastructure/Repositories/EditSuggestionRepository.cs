using Microsoft.EntityFrameworkCore;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Infrastructure.Persistence;

namespace BearNecessities.Infrastructure.Repositories;

public sealed class EditSuggestionRepository : IEditSuggestionRepository
{
    private readonly BearNecessitiesDbContext _context;

    public EditSuggestionRepository(BearNecessitiesDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(EditSuggestion suggestion, CancellationToken cancellationToken = default)
        => await _context.EditSuggestions.AddAsync(suggestion, cancellationToken);

    public Task<EditSuggestion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.EditSuggestions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<List<EditSuggestion>> GetPendingAsync(CancellationToken cancellationToken = default)
        => _context.EditSuggestions
            .Where(s => s.Status == EditSuggestionStatus.Pending)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
}
