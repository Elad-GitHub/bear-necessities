namespace BearNecessities.Infrastructure.Trips;

/// <summary>Sliding one-minute window of AI planner calls shared by all users.</summary>
public sealed class AiPlannerBudget
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private readonly Queue<DateTime> _recentCalls = new();
    private readonly object _gate = new();
    private readonly TimeProvider _clock;

    public AiPlannerBudget(TimeProvider clock) => _clock = clock;

    public bool TryTake(int maxPerMinute)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        lock (_gate)
        {
            while (_recentCalls.Count > 0 && now - _recentCalls.Peek() >= Window)
                _recentCalls.Dequeue();
            if (_recentCalls.Count >= maxPerMinute)
                return false;

            _recentCalls.Enqueue(now);
            return true;
        }
    }
}
