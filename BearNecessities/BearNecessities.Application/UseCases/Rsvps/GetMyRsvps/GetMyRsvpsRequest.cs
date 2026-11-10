namespace BearNecessities.Application.UseCases.Rsvps.GetMyRsvps;

public record GetMyRsvpsRequest(int PageSize = 20, Guid? Cursor = null);
