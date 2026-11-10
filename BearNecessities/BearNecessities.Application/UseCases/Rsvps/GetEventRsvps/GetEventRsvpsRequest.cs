namespace BearNecessities.Application.UseCases.Rsvps.GetEventRsvps;

public record GetEventRsvpsRequest(Guid EventId, int PageSize = 20, Guid? Cursor = null);
