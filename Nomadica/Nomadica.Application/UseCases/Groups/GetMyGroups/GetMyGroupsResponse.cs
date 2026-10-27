using Nomadica.Application.DTOs.Groups;

namespace Nomadica.Application.UseCases.Groups.GetMyGroups;

public record GetMyGroupsResponse(IReadOnlyList<GroupSummaryDto> Items);
