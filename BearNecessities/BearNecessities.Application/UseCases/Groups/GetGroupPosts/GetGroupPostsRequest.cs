namespace BearNecessities.Application.UseCases.Groups.GetGroupPosts;

public record GetGroupPostsRequest(Guid GroupId, int PageSize = 20, Guid? Cursor = null);
