using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.Services.Trips;

namespace BearNecessities.Infrastructure.Trips;

/// <summary>
/// Plans with Gemini when it is configured and within its per-minute budget; on no key, a busy budget,
/// a slow answer, an error or an unusable answer, the traveller gets the smart (non-AI) plan instead.
/// </summary>
public sealed class FallbackItineraryPlanner : IItineraryPlanner
{
    private readonly GeminiItineraryPlanner _gemini;
    private readonly SmartItineraryPlanner _smart;
    private readonly AiPlannerBudget _budget;
    private readonly GeminiSettings _settings;
    private readonly ILogger<FallbackItineraryPlanner> _logger;

    public FallbackItineraryPlanner(
        GeminiItineraryPlanner gemini,
        SmartItineraryPlanner smart,
        AiPlannerBudget budget,
        IOptions<GeminiSettings> settings,
        ILogger<FallbackItineraryPlanner> logger)
    {
        _gemini = gemini;
        _smart = smart;
        _budget = budget;
        _settings = settings.Value;
        _logger = logger;
    }

    public string Name => $"{GeminiItineraryPlanner.PlannerName}+{SmartItineraryPlanner.PlannerName}";

    public async Task<PlannedItinerary> PlanAsync(ItineraryPlanningInput input, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsEnabled || input.Candidates.Count == 0)
            return _smart.Plan(input);

        if (!_budget.TryTake(_settings.MaxRequestsPerMinute))
        {
            _logger.LogInformation("AI planner busy (over {Limit}/min); using the smart planner", _settings.MaxRequestsPerMinute);
            return _smart.Plan(input);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));
        try
        {
            return await _gemini.PlanAsync(input, timeout.Token);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Gemini planner failed ({Error}); using the smart planner", ex.Message);
            return _smart.Plan(input);
        }
    }
}
