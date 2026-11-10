using BearNecessities.Application.DTOs.Groups;

namespace BearNecessities.Application.UseCases.Groups.GetGroups;

public record GetGroupsResponse(IReadOnlyList<GroupSummaryDto> Items);
