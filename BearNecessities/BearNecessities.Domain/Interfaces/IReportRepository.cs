using BearNecessities.Domain.Entities;

namespace BearNecessities.Domain.Interfaces;

public interface IReportRepository
{
    Task AddAsync(Report report, CancellationToken cancellationToken = default);
}
