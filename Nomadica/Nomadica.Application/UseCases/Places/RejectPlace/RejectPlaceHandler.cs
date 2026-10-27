using System.Text.Json;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Places.RejectPlace;

public sealed class RejectPlaceHandler : IRequestHandler<RejectPlaceRequest, RejectPlaceResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RejectPlaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<RejectPlaceResponse> HandleAsync(RejectPlaceRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasRole(UserRole.Admin))
            throw new ForbiddenException();

        var place = await _unitOfWork.Places.GetByIdWithRelationsAsync(new PlaceId(request.PlaceId), cancellationToken)
            ?? throw new NotFoundException("Place", request.PlaceId);

        place.Reject(_currentUser.UserId!);

        var changeLog = ListingChangeLog.Create(
            ListingChangeTargetType.Place,
            place.Id.Value,
            _currentUser.UserId!,
            ChangeType.StatusChange,
            newData: JsonSerializer.Serialize(new { Status = place.Status.ToString(), Reason = request.Reason }));
        await _unitOfWork.ChangeLogs.AddAsync(changeLog, cancellationToken);

        await _unitOfWork.Places.UpdateAsync(place, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RejectPlaceResponse(place.Id.Value, place.Status.ToString());
    }
}
