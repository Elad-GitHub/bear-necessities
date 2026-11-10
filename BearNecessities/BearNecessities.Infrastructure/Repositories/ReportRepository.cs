using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Infrastructure.Persistence;

namespace BearNecessities.Infrastructure.Repositories;

public sealed class ReportRepository : IReportRepository
{
    private readonly BearNecessitiesDbContext _context;

    public ReportRepository(BearNecessitiesDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Report report, CancellationToken cancellationToken = default)
        => await _context.Reports.AddAsync(report, cancellationToken);
}
