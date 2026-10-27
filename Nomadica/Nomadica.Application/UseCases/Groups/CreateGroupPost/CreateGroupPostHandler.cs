using Nomadica.Application.DTOs.Groups;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Groups.CreateGroupPost;

public sealed class CreateGroupPostHandler : IRequestHandler<CreateGroupPostRequest, CreateGroupPostResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateGroupPostHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<CreateGroupPostResponse> HandleAsync(CreateGroupPostRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        if (string.IsNullOrWhiteSpace(request.Body))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["body"] = ["Post body cannot be empty."]
            });

        if (request.Body.Length > GroupPost.MaxBodyLength)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["body"] = [$"Post body cannot exceed {GroupPost.MaxBodyLength} characters."]
            });

        var groupId = new GroupId(request.GroupId);
        _ = await _unitOfWork.Groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group", request.GroupId);

        var membership = await _unitOfWork.Groups.GetMembershipAsync(groupId, _currentUser.UserId, cancellationToken);
        if (membership is null)
            throw new ForbiddenException();

        var post = GroupPost.Create(groupId, _currentUser.UserId, request.Body);
        await _unitOfWork.Groups.AddPostAsync(post, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var author = await _unitOfWork.Users.GetByIdAsync(_currentUser.UserId, cancellationToken);

        return new CreateGroupPostResponse(new GroupPostDto(
            Id: post.Id,
            AuthorId: post.AuthorId.Value,
            AuthorDisplayName: author?.DisplayName ?? "Me",
            AuthorAvatarUrl: author?.AvatarUrl,
            Body: post.Body,
            CreatedAt: post.CreatedAt));
    }
}
