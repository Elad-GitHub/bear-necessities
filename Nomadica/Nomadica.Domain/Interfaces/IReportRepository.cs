using Nomadica.Domain.Entities;

namespace Nomadica.Domain.Interfaces;

public interface IReportRepository
{
    Task AddAsync(Report report, CancellationToken cancellationToken = default);
}
