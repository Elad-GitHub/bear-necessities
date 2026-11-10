using BearNecessities.Application.DTOs.Events;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Events.GetEventsByPlace;

public sealed class GetEventsByPlaceHandler : IRequestHandler<GetEventsByPlaceRequest, GetEventsByPlaceResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetEventsByPlaceHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetEventsByPlaceResponse> HandleAsync(GetEventsByPlaceRequest request, CancellationToken cancellationToken = default)
    {
        var placeId = new PlaceId(request.PlaceId);
        var place = await _unitOfWork.Places.GetByIdAsync(placeId, cancellationToken)
            ?? throw new NotFoundException("Place", request.PlaceId);

        var events = await _unitOfWork.Events.FindUpcomingByPlaceAsync(placeId, cancellationToken);

        var dtos = events.Select(evt => new EventSummaryDto(
            Id: evt.Id.Value,
            Title: evt.Title,
            PlaceId: evt.PlaceId?.Value,
            Latitude: null,
            Longitude: null,
            StartAt: evt.StartAt,
            EndAt: evt.EndAt,
            EventType: evt.EventType,
            IsFree: evt.IsFree,
            Price: evt.Price,
            Currency: evt.Currency,
            Status: evt.Status,
            CategoryIds: evt.Categories.Select(c => c.Id.Value).ToList(),
            ThumbnailUrl: evt.Media.OrderBy(m => m.SortOrder).FirstOrDefault()?.ThumbnailUrl,
            AverageRating: null)).ToList();

        return new GetEventsByPlaceResponse(dtos);
    }
}
