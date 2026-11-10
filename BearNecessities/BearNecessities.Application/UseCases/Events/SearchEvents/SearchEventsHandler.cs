using BearNecessities.Application.Common;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Events.SearchEvents;

public sealed class SearchEventsHandler : IRequestHandler<SearchEventsRequest, SearchEventsResponse>
{
    private const int MinQueryLength = 2;
    private const int MaxLimit = 20;
    private readonly IUnitOfWork _unitOfWork;

    public SearchEventsHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SearchEventsResponse> HandleAsync(SearchEventsRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query) || request.Query.Trim().Length < MinQueryLength)
            return new SearchEventsResponse([]);

        var limit = Math.Clamp(request.Limit, 1, MaxLimit);
        var events = await _unitOfWork.Events.SearchUpcomingByTextAsync(request.Query.Trim(), limit, DateTime.UtcNow, cancellationToken);
        return new SearchEventsResponse(events.Select(EventMapping.ToSummaryDto).ToList());
    }
}
