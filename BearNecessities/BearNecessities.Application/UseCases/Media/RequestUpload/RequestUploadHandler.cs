using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;
using BearNecessities.Shared.Constants;
using MediaEntity = BearNecessities.Domain.Entities.Media;

namespace BearNecessities.Application.UseCases.Media.RequestUpload;

public sealed class RequestUploadHandler : IRequestHandler<RequestUploadRequest, RequestUploadResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IStorageService _storage;
    private readonly StorageSettings _settings;

    public RequestUploadHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IStorageService storage,
        StorageSettings settings)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _storage = storage;
        _settings = settings;
    }

    public async Task<RequestUploadResponse> HandleAsync(RequestUploadRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        ValidateRequest(request);
        await VerifyOwnershipAsync(request, cancellationToken);

        var ext = Path.GetExtension(request.FileName).TrimStart('.');
        var objectKey = $"{request.OwnerType.ToString().ToLowerInvariant()}/{request.OwnerId}/{Guid.NewGuid()}.{ext}";

        var presigned = await _storage.GeneratePresignedUploadUrlAsync(objectKey, request.MimeType, request.FileSizeBytes, cancellationToken);

        var media = MediaEntity.Create(
            ownerType: request.OwnerType,
            ownerId: request.OwnerId,
            storagePath: objectKey,
            url: presigned.CdnUrl,
            mimeType: request.MimeType,
            sizeBytes: request.FileSizeBytes,
            uploadedBy: _currentUser.UserId,
            sortOrder: 0);

        await _unitOfWork.Media.AddAsync(media, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RequestUploadResponse(media.Id, presigned.UploadUrl, objectKey, presigned.CdnUrl, presigned.ExpiresAt);
    }

    private void ValidateRequest(RequestUploadRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.FileName))
            errors["fileName"] = ["FileName is required."];

        if (string.IsNullOrWhiteSpace(request.MimeType))
            errors["mimeType"] = ["MimeType is required."];
        else if (!_settings.AllowedMimeTypes.Contains(request.MimeType))
            errors["mimeType"] = [$"MimeType '{request.MimeType}' is not allowed. Allowed types: {string.Join(", ", _settings.AllowedMimeTypes)}."];

        if (request.FileSizeBytes <= 0)
            errors["fileSizeBytes"] = ["FileSizeBytes must be greater than zero."];
        else if (request.FileSizeBytes > _settings.MaxFileSizeBytes)
            errors["fileSizeBytes"] = [$"File size exceeds maximum of {_settings.MaxFileSizeBytes / 1_048_576}MB."];

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }

    private async Task VerifyOwnershipAsync(RequestUploadRequest request, CancellationToken cancellationToken)
    {
        var isAdmin = _currentUser.HasRole(UserRole.Admin);
        if (isAdmin)
            return;

        switch (request.OwnerType)
        {
            case MediaOwnerType.Place:
            {
                // Galleries are community-built: any member can add a photo, which waits for an admin (see ConfirmUpload)
                _ = await _unitOfWork.Places.GetByIdAsync(new PlaceId(request.OwnerId), cancellationToken)
                    ?? throw new NotFoundException("Place", request.OwnerId);
                break;
            }
            case MediaOwnerType.Event:
            {
                var evt = await _unitOfWork.Events.GetByIdAsync(new EventId(request.OwnerId), cancellationToken)
                    ?? throw new NotFoundException("Event", request.OwnerId);
                if (evt.OwnerId != _currentUser.UserId)
                    throw new ForbiddenException();
                break;
            }
            case MediaOwnerType.User:
            {
                if (_currentUser.UserId!.Value != request.OwnerId)
                    throw new ForbiddenException();
                break;
            }
        }
    }
}
