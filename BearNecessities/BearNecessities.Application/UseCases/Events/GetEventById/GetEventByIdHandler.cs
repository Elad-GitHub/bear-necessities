using BearNecessities.Application.DTOs;
using BearNecessities.Application.DTOs.Events;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Events.GetEventById;

public sealed class GetEventByIdHandler : IRequestHandler<GetEventByIdRequest, EventDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetEventByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<EventDetailDto> HandleAsync(GetEventByIdRequest request, CancellationToken cancellationToken = default)
    {
        var evt = await _unitOfWork.Events.GetByIdWithRelationsAsync(new EventId(request.EventId), cancellationToken)
            ?? throw new NotFoundException("Event", request.EventId);

        var upcoming = await _unitOfWork.Occurrences.GetUpcomingByEventAsync(evt.Id, count: 10, cancellationToken);
        // Events borrow the photo of the place they happen at
        var venue = evt.PlaceId is null ? null : await _unitOfWork.Places.GetByIdAsync(evt.PlaceId, cancellationToken);

        return new EventDetailDto(
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
            AverageRating: null,
            Description: evt.Description,
            RRule: evt.RRule,
            ExDates: evt.ExDates.ToList(),
            OnlineUrl: evt.OnlineUrl,
            MaxCapacity: evt.MaxCapacity,
            Timezone: evt.Timezone,
            CreatedAt: evt.CreatedAt,
            Categories: evt.Categories
                .Select(c => new CategoryDto(c.Id.Value, c.Slug, c.Name, c.Icon, c.Color))
                .ToList(),
            Media: evt.Media
                .OrderBy(m => m.SortOrder)
                .Select(m => new MediaDto(m.Id, m.Url, m.ThumbnailUrl, m.MimeType, m.SortOrder, m.ModerationStatus))
                .ToList(),
            UpcomingOccurrences: upcoming
                .Select(o => new OccurrenceDto(o.Id, o.StartAt, o.EndAt, o.Status.ToString()))
                .ToList(),
            PhotoUrl: venue?.PhotoUrl,
            PhotoCredit: venue?.PhotoCredit,
            PhotoCreditUrl: venue?.PhotoCreditUrl);
    }
}
