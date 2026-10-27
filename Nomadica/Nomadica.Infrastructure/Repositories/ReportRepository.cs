using Nomadica.Domain.Entities;
using Nomadica.Domain.Interfaces;
using Nomadica.Infrastructure.Persistence;

namespace Nomadica.Infrastructure.Repositories;

public sealed class ReportRepository : IReportRepository
{
    private readonly NomadiaDbContext _context;

    public ReportRepository(NomadiaDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Report report, CancellationToken cancellationToken = default)
        => await _context.Reports.AddAsync(report, cancellationToken);
}
