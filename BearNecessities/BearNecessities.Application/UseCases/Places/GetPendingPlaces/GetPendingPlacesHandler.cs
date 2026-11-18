using BearNecessities.Application.DTOs.Places;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Places.GetPendingPlaces;

public sealed class GetPendingPlacesHandler : IRequestHandler<GetPendingPlacesRequest, GetPendingPlacesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetPendingPlacesHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GetPendingPlacesResponse> HandleAsync(GetPendingPlacesRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasRole(UserRole.Admin) && !_currentUser.HasRole(UserRole.Steward))
            throw new ForbiddenException();

        var places = await _unitOfWork.Places.GetPendingAsync(request.PageSize, request.Cursor, cancellationToken);

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
                ThumbnailUrl: p.PhotoUrl,
                IsPermanent: p.IsPermanent))
            .ToList();

        var nextCursor = places.Count == request.PageSize
            ? places[^1].Id.Value
            : (Guid?)null;

        return new GetPendingPlacesResponse(items, nextCursor);
    }
}
