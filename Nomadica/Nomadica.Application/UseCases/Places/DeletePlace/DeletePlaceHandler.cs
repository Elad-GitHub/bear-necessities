using System.Text.Json;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Places.DeletePlace;

public sealed class DeletePlaceHandler : IRequestHandler<DeletePlaceRequest, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public DeletePlaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(DeletePlaceRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var place = await _unitOfWork.Places.GetByIdWithRelationsAsync(new PlaceId(request.PlaceId), cancellationToken)
            ?? throw new NotFoundException("Place", request.PlaceId);

        var isOwner = place.OwnerId == _currentUser.UserId;
        var isAdmin = _currentUser.HasRole(UserRole.Admin);

        if (!isOwner && !isAdmin)
            throw new ForbiddenException();

        place.Archive();

        var changeLog = ListingChangeLog.Create(
            ListingChangeTargetType.Place,
            place.Id.Value,
            _currentUser.UserId,
            ChangeType.StatusChange,
            newData: JsonSerializer.Serialize(new { Status = place.Status.ToString() }));
        await _unitOfWork.ChangeLogs.AddAsync(changeLog, cancellationToken);

        await _unitOfWork.Places.UpdateAsync(place, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
