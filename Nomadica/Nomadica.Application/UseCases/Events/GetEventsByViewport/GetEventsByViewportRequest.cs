namespace Nomadica.Application.UseCases.Events.GetEventsByViewport;

public record GetEventsByViewportRequest(
    decimal NeLat,
    decimal NeLng,
    decimal SwLat,
    decimal SwLng,
    List<Guid>? CategoryIds,
    DateTime? DateFrom,
    DateTime? DateTo,
    int PageSize,
    Guid? Cursor);
