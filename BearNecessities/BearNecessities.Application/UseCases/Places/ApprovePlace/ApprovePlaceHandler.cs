using System.Text.Json;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Places.ApprovePlace;

public sealed class ApprovePlaceHandler : IRequestHandler<ApprovePlaceRequest, ApprovePlaceResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ApprovePlaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<ApprovePlaceResponse> HandleAsync(ApprovePlaceRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasRole(UserRole.Admin))
            throw new ForbiddenException();

        var place = await _unitOfWork.Places.GetByIdWithRelationsAsync(new PlaceId(request.PlaceId), cancellationToken)
            ?? throw new NotFoundException("Place", request.PlaceId);

        place.Approve(_currentUser.UserId!);

        var changeLog = ListingChangeLog.Create(
            ListingChangeTargetType.Place,
            place.Id.Value,
            _currentUser.UserId!,
            ChangeType.StatusChange,
            newData: JsonSerializer.Serialize(new { Status = place.Status.ToString(), place.ReviewedAt }));
        await _unitOfWork.ChangeLogs.AddAsync(changeLog, cancellationToken);

        await _unitOfWork.Places.UpdateAsync(place, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ApprovePlaceResponse(place.Id.Value, place.Status.ToString());
    }
}
