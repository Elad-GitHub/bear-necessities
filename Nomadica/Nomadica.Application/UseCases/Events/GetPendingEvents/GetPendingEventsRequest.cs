namespace Nomadica.Application.UseCases.Events.GetPendingEvents;

public record GetPendingEventsRequest(int PageSize = 20, Guid? Cursor = null);
