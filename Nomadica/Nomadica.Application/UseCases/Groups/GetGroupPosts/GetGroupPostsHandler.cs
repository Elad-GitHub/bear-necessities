using Nomadica.Application.DTOs.Groups;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Groups.GetGroupPosts;

public sealed class GetGroupPostsHandler : IRequestHandler<GetGroupPostsRequest, GetGroupPostsResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetGroupPostsHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetGroupPostsResponse> HandleAsync(GetGroupPostsRequest request, CancellationToken cancellationToken = default)
    {
        var groupId = new GroupId(request.GroupId);
        _ = await _unitOfWork.Groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group", request.GroupId);

        var posts = await _unitOfWork.Groups.GetPostsByGroupAsync(
            groupId, request.PageSize, request.Cursor, cancellationToken);

        var authorIds = posts.Select(p => p.AuthorId).Distinct().ToList();
        var authors = await _unitOfWork.Users.GetByIdsAsync(authorIds, cancellationToken);
        var authorMap = authors.ToDictionary(u => u.Id.Value);

        var items = posts.Select(post =>
        {
            authorMap.TryGetValue(post.AuthorId.Value, out var author);
            return new GroupPostDto(
                Id: post.Id,
                AuthorId: post.AuthorId.Value,
                AuthorDisplayName: author?.DisplayName ?? "Former member",
                AuthorAvatarUrl: author?.AvatarUrl,
                Body: post.Body,
                CreatedAt: post.CreatedAt);
        }).ToList();

        var nextCursor = posts.Count == request.PageSize ? posts[^1].Id : (Guid?)null;
        return new GetGroupPostsResponse(items, nextCursor);
    }
}
