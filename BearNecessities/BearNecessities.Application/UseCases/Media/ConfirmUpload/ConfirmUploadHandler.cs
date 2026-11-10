using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Media.ConfirmUpload;

public sealed class ConfirmUploadHandler : IRequestHandler<ConfirmUploadRequest, ConfirmUploadResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IStorageService _storage;

    public ConfirmUploadHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, IStorageService storage)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<ConfirmUploadResponse> HandleAsync(ConfirmUploadRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var media = await _unitOfWork.Media.GetByIdAsync(request.MediaId, cancellationToken)
            ?? throw new NotFoundException("Media", request.MediaId);

        var isAdmin = _currentUser.HasRole(UserRole.Admin);
        if (media.UploadedBy != _currentUser.UserId && !isAdmin)
            throw new ForbiddenException();

        var exists = await _storage.ObjectExistsAsync(media.StoragePath, cancellationToken);
        if (!exists)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["objectKey"] = ["Upload not found in storage. Please re-upload the file."]
            });

        media.SetModerationStatus(ModerationStatus.Approved);
        await _unitOfWork.Media.UpdateAsync(media, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ConfirmUploadResponse(media.Id, media.Url, media.ModerationStatus.ToString());
    }
}
