using BearNecessities.Application.DTOs.Groups;

namespace BearNecessities.Application.UseCases.Groups.GetGroupPosts;

public record GetGroupPostsResponse(IReadOnlyList<GroupPostDto> Items, Guid? NextCursor);
