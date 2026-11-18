using BearNecessities.Application.DTOs;
using BearNecessities.Application.DTOs.Places;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Places.GetPlaceById;

public sealed class GetPlaceByIdHandler
    : IRequestHandler<GetPlaceByIdRequest, GetPlaceByIdResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPlaceByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetPlaceByIdResponse> HandleAsync(
        GetPlaceByIdRequest request,
        CancellationToken cancellationToken)
    {
        var place = await _unitOfWork.Places.GetByIdWithRelationsAsync(
            new PlaceId(request.PlaceId), cancellationToken);

        if (place is null)
            return new GetPlaceByIdResponse(null);

        var media = await _unitOfWork.Media.GetByOwnersAsync(
            MediaOwnerType.Place, [place.Id.Value], cancellationToken);

        // Members' photos wait for an admin before they reach the banner or the gallery
        var mediaDtos = media
            .Where(m => m.ModerationStatus == ModerationStatus.Approved)
            .Select(m => new MediaDto(m.Id, m.Url, m.ThumbnailUrl, m.MimeType, m.SortOrder, m.ModerationStatus))
            .ToList();

        return new GetPlaceByIdResponse(new PlaceDetailDto(
            Id: place.Id.Value,
            Name: place.Name,
            Latitude: place.Latitude,
            Longitude: place.Longitude,
            H3IndexRes8: place.H3IndexRes8,
            City: place.City,
            Country: place.Country,
            Status: place.Status,
            AverageRating: null, // requires a dedicated review-aggregate query; not yet implemented
            ReviewCount: 0,
            CategoryIds: place.Categories.Select(c => c.Id.Value).ToList(),
            ThumbnailUrl: media.Count > 0 ? media[0].ThumbnailUrl : null,
            IsPermanent: place.IsPermanent,
            Description: place.Description,
            Address: place.Address,
            Timezone: place.Timezone,
            Source: place.Source,
            IsVerified: place.IsVerified,
            ExternalLinks: place.ExternalLinks
                .Select(el => new ExternalLinkDto(el.Id, el.Label, el.Url, el.SortOrder))
                .ToList(),
            Categories: place.Categories
                .Select(c => new CategoryDto(c.Id.Value, c.Slug, c.Name, c.Icon, c.Color))
                .ToList(),
            Media: mediaDtos,
            CreatedAt: place.CreatedAt,
            PhotoUrl: place.PhotoUrl,
            PhotoCredit: place.PhotoCredit,
            PhotoCreditUrl: place.PhotoCreditUrl));
    }
}
