using BearNecessities.Infrastructure.Imports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BearNecessities.Infrastructure.Services;

/// <summary>
/// Looks for a Wikimedia photo for places nobody has searched yet, a small batch at a time, so newly added
/// places get a picture within minutes without anyone running find-photos.
/// </summary>
public sealed class PlacePhotoBackgroundService : BackgroundService
{
    private const int PlacesPerRound = 40;
    private static readonly TimeSpan RoundInterval = TimeSpan.FromMinutes(10);
    // Lets the API finish starting up before the first round
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PlacePhotoBackgroundService> _logger;

    public PlacePhotoBackgroundService(IServiceScopeFactory scopeFactory, ILogger<PlacePhotoBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await SearchRoundAsync(stoppingToken);
                await Task.Delay(RoundInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Place photo search stopped");
        }
    }

    private async Task SearchRoundAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var photos = scope.ServiceProvider.GetRequiredService<PlacePhotoService>();
            var (checkedCount, foundCount) = await photos.FindMissingPhotosAsync(null, PlacesPerRound, stoppingToken);
            if (checkedCount > 0)
                _logger.LogInformation("Place photo search: {Found} of {Checked} new places got a photo", foundCount, checkedCount);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Place photo search round failed; trying again next round");
        }
    }
}
