using Nomadica.Application.Common;
using Nomadica.Application.DTOs.Places;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;

namespace Nomadica.Application.UseCases.Places.GetMyPlaces;

public sealed class GetMyPlacesHandler : IRequestHandler<GetMyPlacesRequest, GetMyPlacesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetMyPlacesHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GetMyPlacesResponse> HandleAsync(GetMyPlacesRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var places = await _unitOfWork.Places.GetByOwnerAsync(_currentUser.UserId, cancellationToken);

        var suspendedIds = places
            .Where(p => p.Status == ListingStatus.Suspended)
            .Select(p => p.Id.Value)
            .ToList();

        var rejectionReasons = await GetRejectionReasonsAsync(suspendedIds, cancellationToken);

        var items = places
            .Select(p => new PlaceSummaryDto(
                Id: p.Id.Value,
                Name: p.Name,
                Latitude: p.Latitude,
                Longitude: p.Longitude,
                H3IndexRes8: p.H3IndexRes8,
                City: p.City,
                Country: p.Country,
                Status: p.Status,
                AverageRating: null,
                ReviewCount: 0,
                CategoryIds: p.Categories.Select(c => c.Id.Value).ToList(),
                ThumbnailUrl: null,
                IsPermanent: p.IsPermanent,
                RejectionReason: rejectionReasons.GetValueOrDefault(p.Id.Value)))
            .ToList();

        return new GetMyPlacesResponse(items);
    }

    private async Task<Dictionary<Guid, string?>> GetRejectionReasonsAsync(
        List<Guid> suspendedPlaceIds,
        CancellationToken cancellationToken)
    {
        if (suspendedPlaceIds.Count == 0) return [];

        var statusChanges = await _unitOfWork.ChangeLogs.GetLatestStatusChangesAsync(
            ListingChangeTargetType.Place, suspendedPlaceIds, cancellationToken);

        return statusChanges.ToDictionary(
            log => log.TargetId,
            log => ChangeLogReasonParser.ParseReason(log.NewData));
    }
}
