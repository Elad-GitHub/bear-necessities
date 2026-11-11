using System.Text.Json;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Places.UpdatePlace;

/// <summary>
/// Admins edit places directly, and so do owners while their place isn't live yet. Anyone else's edit,
/// owners of live places included, becomes a suggestion that waits for an admin; the place stays as it is.
/// </summary>
public sealed class UpdatePlaceHandler : IRequestHandler<UpdatePlaceRequest, UpdatePlaceResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdatePlaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<UpdatePlaceResponse> HandleAsync(UpdatePlaceRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var place = await _unitOfWork.Places.GetByIdWithRelationsAsync(new PlaceId(request.PlaceId), cancellationToken)
            ?? throw new NotFoundException("Place", request.PlaceId);

        PlaceEdits.Validate(request);

        var isOwnDraft = place.OwnerId == _currentUser.UserId && place.Status != ListingStatus.Live;
        if (!_currentUser.HasRole(UserRole.Admin) && !isOwnDraft)
        {
            var suggestion = EditSuggestion.Create(
                ListingChangeTargetType.Place, place.Id.Value, _currentUser.UserId, JsonSerializer.Serialize(request));
            await _unitOfWork.EditSuggestions.AddAsync(suggestion, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new UpdatePlaceResponse(place.Id.Value, place.Status.ToString(), RequiresReview: true);
        }

        await PlaceEdits.ApplyAsync(_unitOfWork, place, request, _currentUser.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdatePlaceResponse(place.Id.Value, place.Status.ToString(), RequiresReview: false);
    }
}
