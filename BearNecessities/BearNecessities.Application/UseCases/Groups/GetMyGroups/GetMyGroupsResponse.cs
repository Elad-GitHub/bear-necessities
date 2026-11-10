using BearNecessities.Application.DTOs.Groups;

namespace BearNecessities.Application.UseCases.Groups.GetMyGroups;

public record GetMyGroupsResponse(IReadOnlyList<GroupSummaryDto> Items);
