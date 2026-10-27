using Nomadica.Application.DTOs.Events;

namespace Nomadica.Application.UseCases.Events.GetEventsByPlace;

public record GetEventsByPlaceResponse(List<EventSummaryDto> Items);
