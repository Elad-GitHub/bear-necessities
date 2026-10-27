using Nomadica.Application.DTOs.Groups;

namespace Nomadica.Application.UseCases.Groups.GetGroups;

public record GetGroupsResponse(IReadOnlyList<GroupSummaryDto> Items);
