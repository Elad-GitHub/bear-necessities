namespace Nomadica.Application.Interfaces;

public interface IOccurrenceRefreshService
{
    Task<int> RefreshWindowAsync(CancellationToken cancellationToken = default);
}
