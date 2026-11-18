using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Media.ModeratePhotos;

public sealed record GetPendingPhotosRequest;

/// <param name="ReviewSnippet">The start of the review the photo came with, if any, so the admin sees its context.</param>
public sealed record PendingPhotoDto(
    Guid Id, Guid PlaceId, string PlaceName, string UploadedByName, DateTime UploadedAt, string? ReviewSnippet);

public sealed record GetPendingPhotosResponse(List<PendingPhotoDto> Items);

public sealed record ReviewPhotoRequest(Guid MediaId, bool IsApproved);

public sealed record ReviewPhotoResponse(Guid MediaId, string Status);

/// <summary>Gallery photos members added, waiting for an admin in Paw of Approval.</summary>
public sealed class GetPendingPhotosHandler : IRequestHandler<GetPendingPhotosRequest, GetPendingPhotosResponse>
{
    private const int SnippetLength = 120;
    private const string UnknownName = "Unknown";

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetPendingPhotosHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GetPendingPhotosResponse> HandleAsync(GetPendingPhotosRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasRole(UserRole.Admin))
            throw new ForbiddenException();

        var photos = await _unitOfWork.Media.GetPendingAsync(cancellationToken);
        if (photos.Count == 0) return new GetPendingPhotosResponse([]);

        var placeNames = (await _unitOfWork.Places.GetByIdsAsync(photos.Select(p => new PlaceId(p.OwnerId)).Distinct(), cancellationToken))
            .ToDictionary(place => place.Id.Value, place => place.Name);
        var uploaderNames = (await _unitOfWork.Users.GetByIdsAsync(photos.Select(p => p.UploadedBy).Distinct(), cancellationToken))
            .ToDictionary(user => user.Id.Value, user => user.DisplayName);
        var reviewIds = photos.Where(p => p.ReviewId.HasValue).Select(p => p.ReviewId!.Value).Distinct().ToList();
        var reviewBodies = reviewIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : (await _unitOfWork.Reviews.GetByIdsAsync(reviewIds, cancellationToken)).ToDictionary(review => review.Id, review => review.Body);

        var items = photos.Select(photo => new PendingPhotoDto(
            photo.Id,
            photo.OwnerId,
            placeNames.GetValueOrDefault(photo.OwnerId, UnknownName),
            uploaderNames.GetValueOrDefault(photo.UploadedBy.Value, UnknownName),
            photo.CreatedAt,
            photo.ReviewId is { } reviewId ? Shorten(reviewBodies.GetValueOrDefault(reviewId)) : null)).ToList();

        return new GetPendingPhotosResponse(items);
    }

    private static string? Shorten(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Length <= SnippetLength ? text : text[..SnippetLength].TrimEnd() + "…";
}

/// <summary>An admin approves a member's photo (it joins the gallery) or rejects it (the file is deleted).</summary>
public sealed class ReviewPhotoHandler : IRequestHandler<ReviewPhotoRequest, ReviewPhotoResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IStorageService _storage;

    public ReviewPhotoHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, IStorageService storage)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<ReviewPhotoResponse> HandleAsync(ReviewPhotoRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasRole(UserRole.Admin))
            throw new ForbiddenException();

        var photo = await _unitOfWork.Media.GetByIdAsync(request.MediaId, cancellationToken)
            ?? throw new NotFoundException("Media", request.MediaId);

        if (request.IsApproved)
        {
            photo.SetModerationStatus(ModerationStatus.Approved);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new ReviewPhotoResponse(photo.Id, photo.ModerationStatus.ToString());
        }

        await _storage.DeleteObjectAsync(photo.StoragePath, cancellationToken);
        await _unitOfWork.Media.DeleteAsync(photo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ReviewPhotoResponse(photo.Id, ModerationStatus.Rejected.ToString());
    }
}
