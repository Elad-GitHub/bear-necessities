using Nomadica.Application.DTOs.Events;
using Nomadica.Domain.Entities;

namespace Nomadica.Application.Common;

public static class EventMapping
{
    public static EventSummaryDto ToSummaryDto(Event evt) => new(
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
        CategoryIds: evt.Categories.Select(category => category.Id.Value).ToList(),
        ThumbnailUrl: null,
        AverageRating: null);
}
