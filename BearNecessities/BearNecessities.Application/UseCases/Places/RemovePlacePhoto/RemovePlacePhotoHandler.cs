using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Places.RemovePlacePhoto;

/// <summary>Admins take down a wrong automatic photo; the place falls back to its category image.</summary>
public sealed class RemovePlacePhotoHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RemovePlacePhotoHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(RemovePlacePhotoRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasRole(UserRole.Admin))
            throw new ForbiddenException();

        var place = await _unitOfWork.Places.GetByIdAsync(new PlaceId(request.PlaceId), cancellationToken)
            ?? throw new NotFoundException("Place", request.PlaceId);

        place.RemovePhoto();
        await _unitOfWork.Places.UpdateAsync(place, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
