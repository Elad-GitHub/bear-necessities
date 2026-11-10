using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Media.DeleteMedia;

public sealed class DeleteMediaHandler : IRequestHandler<DeleteMediaRequest, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IStorageService _storage;

    public DeleteMediaHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, IStorageService storage)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<bool> HandleAsync(DeleteMediaRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var media = await _unitOfWork.Media.GetByIdAsync(request.MediaId, cancellationToken)
            ?? throw new NotFoundException("Media", request.MediaId);

        var isAdmin = _currentUser.HasRole(UserRole.Admin);
        if (media.UploadedBy != _currentUser.UserId && !isAdmin)
            throw new ForbiddenException();

        await _storage.DeleteObjectAsync(media.StoragePath, cancellationToken);
        await _unitOfWork.Media.DeleteAsync(media, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
