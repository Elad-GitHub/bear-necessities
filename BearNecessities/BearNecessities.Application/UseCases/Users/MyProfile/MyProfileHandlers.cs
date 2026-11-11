using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Users.MyProfile;

public sealed record GetMyProfileRequest;

/// <param name="AvatarMediaId">A photo the member uploaded as a User media item; null keeps the current photo.</param>
/// <param name="RemoveAvatar">True clears the photo, so initials show instead.</param>
public sealed record UpdateMyProfileRequest(string DisplayName, string? Bio, Guid? AvatarMediaId, bool RemoveAvatar = false);

public sealed record MyProfileDto(Guid Id, string Email, string DisplayName, string? Bio, string? AvatarUrl);

/// <summary>The signed-in member's own profile, for the "Bear-ly Me" page.</summary>
public sealed class GetMyProfileHandler : IRequestHandler<GetMyProfileRequest, MyProfileDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetMyProfileHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<MyProfileDto> HandleAsync(GetMyProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await MyProfile.LoadCurrentUserAsync(_unitOfWork, _currentUser, cancellationToken);
        return MyProfile.ToDto(user);
    }
}

/// <summary>Updates the signed-in member's name, a few words about them, and their photo.</summary>
public sealed class UpdateMyProfileHandler : IRequestHandler<UpdateMyProfileRequest, MyProfileDto>
{
    private const int MaxDisplayNameLength = 100;
    private const int MaxBioLength = 500;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateMyProfileHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<MyProfileDto> HandleAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var user = await MyProfile.LoadCurrentUserAsync(_unitOfWork, _currentUser, cancellationToken);

        user.UpdateProfile(request.DisplayName, request.Bio);

        if (request.RemoveAvatar)
        {
            user.SetAvatarUrl(null);
        }
        else if (request.AvatarMediaId.HasValue)
        {
            var media = await _unitOfWork.Media.GetByIdAsync(request.AvatarMediaId.Value, cancellationToken)
                ?? throw new NotFoundException("Media", request.AvatarMediaId.Value);

            // Only a photo this member uploaded of themselves can become their avatar
            if (media.OwnerType != MediaOwnerType.User || media.OwnerId != user.Id.Value)
                throw new ForbiddenException();

            user.SetAvatarUrl(MyProfile.AvatarPath(media.Id));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MyProfile.ToDto(user);
    }

    private static void Validate(UpdateMyProfileRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            errors["displayName"] = ["Please tell us what to call you."];
        else if (request.DisplayName.Trim().Length > MaxDisplayNameLength)
            errors["displayName"] = [$"Name must not exceed {MaxDisplayNameLength} characters."];

        if (request.Bio?.Length > MaxBioLength)
            errors["bio"] = [$"Keep it under {MaxBioLength} characters."];

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }
}

internal static class MyProfile
{
    // Served by MediaController; stored as a path so it works on any API host
    public static string AvatarPath(Guid mediaId) => $"/api/v1/media/{mediaId}/content";

    public static async Task<User> LoadCurrentUserAsync(
        IUnitOfWork unitOfWork, ICurrentUserService currentUser, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        return await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("User", currentUser.UserId.Value);
    }

    public static MyProfileDto ToDto(User user) =>
        new(user.Id.Value, user.Email, user.DisplayName, user.Bio, user.AvatarUrl);
}
