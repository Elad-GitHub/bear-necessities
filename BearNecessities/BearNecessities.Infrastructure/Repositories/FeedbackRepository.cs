using Microsoft.EntityFrameworkCore;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Infrastructure.Persistence;

namespace BearNecessities.Infrastructure.Repositories;

public sealed class FeedbackRepository : IFeedbackRepository
{
    private readonly BearNecessitiesDbContext _context;

    public FeedbackRepository(BearNecessitiesDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FeedbackMessage feedback, CancellationToken cancellationToken = default)
        => await _context.FeedbackMessages.AddAsync(feedback, cancellationToken);

    public Task<FeedbackMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.FeedbackMessages.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public Task<List<FeedbackMessage>> GetOpenAsync(CancellationToken cancellationToken = default)
        => _context.FeedbackMessages
            .Where(f => f.HandledAt == null)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
}
