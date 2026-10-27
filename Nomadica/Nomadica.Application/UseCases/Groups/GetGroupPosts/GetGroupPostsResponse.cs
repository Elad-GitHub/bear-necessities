using Nomadica.Application.DTOs.Groups;

namespace Nomadica.Application.UseCases.Groups.GetGroupPosts;

public record GetGroupPostsResponse(IReadOnlyList<GroupPostDto> Items, Guid? NextCursor);
