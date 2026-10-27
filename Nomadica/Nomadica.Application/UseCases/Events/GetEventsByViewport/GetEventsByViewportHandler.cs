using Nomadica.Application.DTOs.Events;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Events.GetEventsByViewport;

public sealed class GetEventsByViewportHandler : IRequestHandler<GetEventsByViewportRequest, GetEventsByViewportResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetEventsByViewportHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetEventsByViewportResponse> HandleAsync(GetEventsByViewportRequest request, CancellationToken cancellationToken = default)
    {
        if (request.PageSize is < 1 or > 100)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["pageSize"] = ["PageSize must be between 1 and 100."]
            });

        Coordinates neCoords;
        Coordinates swCoords;
        try
        {
            neCoords = Coordinates.Create(request.NeLat, request.NeLng);
            swCoords = Coordinates.Create(request.SwLat, request.SwLng);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["viewport"] = [ex.Message]
            });
        }

        var viewport = new GeoViewport(neCoords, swCoords);
        var categoryIds = request.CategoryIds?
            .Select(id => new CategoryId(id))
            .ToList();

        var (items, totalCount) = await _unitOfWork.Events.FindByViewportPagedAsync(
            viewport, categoryIds, request.DateFrom, request.DateTo,
            request.PageSize, request.Cursor, cancellationToken);

        var dtos = items.Select(item => new EventSummaryDto(
            Id: item.Event.Id.Value,
            Title: item.Event.Title,
            PlaceId: item.Event.PlaceId?.Value,
            Latitude: item.Latitude,
            Longitude: item.Longitude,
            StartAt: item.Event.StartAt,
            EndAt: item.Event.EndAt,
            EventType: item.Event.EventType,
            IsFree: item.Event.IsFree,
            Price: item.Event.Price,
            Currency: item.Event.Currency,
            Status: item.Event.Status,
            CategoryIds: item.Event.Categories.Select(c => c.Id.Value).ToList(),
            ThumbnailUrl: item.Event.Media.OrderBy(m => m.SortOrder).FirstOrDefault()?.ThumbnailUrl,
            AverageRating: null,
            Description: item.Event.Description)).ToList();

        Guid? nextCursor = items.Count == request.PageSize ? items[^1].Event.Id.Value : null;

        return new GetEventsByViewportResponse(dtos, totalCount, nextCursor);
    }
}
