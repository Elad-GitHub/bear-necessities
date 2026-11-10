using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BearNecessities.Application.Interfaces;

namespace BearNecessities.Infrastructure.Services;

public sealed class OccurrenceRefreshBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OccurrenceRefreshBackgroundService> _logger;

    public OccurrenceRefreshBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<OccurrenceRefreshBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OccurrenceRefreshBackgroundService started");

        var consecutiveErrors = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var refreshService = scope.ServiceProvider.GetRequiredService<IOccurrenceRefreshService>();
                var count = await refreshService.RefreshWindowAsync(stoppingToken);
                _logger.LogInformation("Occurrence refresh cycle completed: {Count} events processed", count);
                consecutiveErrors = 0;

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("OccurrenceRefreshBackgroundService shutting down");
                break;
            }
            catch (Exception ex)
            {
                consecutiveErrors++;
                _logger.LogError(ex, "Error in occurrence refresh cycle ({ErrorCount} consecutive errors)", consecutiveErrors);

                if (consecutiveErrors >= 5)
                {
                    _logger.LogError("Too many consecutive errors; waiting longer before retry");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        _logger.LogInformation("OccurrenceRefreshBackgroundService stopped");
    }
}
