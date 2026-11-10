namespace BearNecessities.Application.UseCases.Groups.GetGroups;

public record GetGroupsRequest(string? Query = null, int Limit = 50);
