using System.Text.Json;
using Nomadica.Application.DTOs.Places;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Places.UpdatePlace;

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

        var isOwner = place.OwnerId == _currentUser.UserId;
        var isAdmin = _currentUser.HasRole(UserRole.Admin);

        if (!isOwner && !isAdmin)
            throw new ForbiddenException();

        Validate(request);

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            place.Name, place.Latitude, place.Longitude,
            Status = place.Status.ToString(),
            CategoryIds = place.Categories.Select(c => c.Id.Value)
        });

        var isMajorChange = DetermineIsMajorChange(request, place);

        ApplyFieldUpdates(request, place);

        if (request.CategoryIds is not null)
        {
            var categoryIds = request.CategoryIds.Select(id => new CategoryId(id)).ToList();
            var allCategories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
            var foundCategories = allCategories
                .Where(c => categoryIds.Any(id => id == c.Id))
                .ToList();

            if (foundCategories.Count != categoryIds.Count)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["categoryIds"] = ["One or more category IDs were not found."]
                });

            place.ClearCategories();
            foreach (var category in foundCategories)
                place.AddCategory(category);
        }

        if (request.ExternalLinks is not null)
        {
            var newLinks = request.ExternalLinks
                .Select((dto, i) => PlaceExternalLink.Create(place.Id, dto.Label, dto.Url, i))
                .ToList();
            await _unitOfWork.Places.ReplaceExternalLinksAsync(place.Id, newLinks, cancellationToken);
        }

        var requiresReview = false;
        if (isMajorChange && place.Status is ListingStatus.Live or ListingStatus.Suspended)
        {
            place.RequestReview();
            requiresReview = true;
        }

        var changeLog = ListingChangeLog.Create(
            ListingChangeTargetType.Place,
            place.Id.Value,
            _currentUser.UserId,
            isMajorChange ? ChangeType.MajorEdit : ChangeType.MinorEdit,
            oldData: oldSnapshot,
            newData: JsonSerializer.Serialize(new
            {
                place.Name, place.Latitude, place.Longitude,
                Status = place.Status.ToString(),
                CategoryIds = place.Categories.Select(c => c.Id.Value)
            }));
        await _unitOfWork.ChangeLogs.AddAsync(changeLog, cancellationToken);

        await _unitOfWork.Places.UpdateAsync(place, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdatePlaceResponse(place.Id.Value, place.Status.ToString(), requiresReview);
    }

    private static bool DetermineIsMajorChange(UpdatePlaceRequest request, Place place)
    {
        if (request.Name is not null && request.Name.Trim() != place.Name)
            return true;

        if (request.Latitude.HasValue || request.Longitude.HasValue)
        {
            var newLat = request.Latitude ?? place.Latitude;
            var newLng = request.Longitude ?? place.Longitude;
            if (newLat != place.Latitude || newLng != place.Longitude)
                return true;
        }

        if (request.CategoryIds is not null)
        {
            var existing = place.Categories.Select(c => c.Id.Value).OrderBy(id => id).ToList();
            var incoming = request.CategoryIds.OrderBy(id => id).ToList();
            if (!existing.SequenceEqual(incoming))
                return true;
        }

        return false;
    }

    private static void ApplyFieldUpdates(UpdatePlaceRequest request, Place place)
    {
        if (request.Name is not null)
            place.SetName(request.Name);

        if (request.Description is not null)
            place.SetDescription(request.Description);

        if (request.Latitude.HasValue || request.Longitude.HasValue)
        {
            var lat = request.Latitude ?? place.Latitude;
            var lng = request.Longitude ?? place.Longitude;
            var h3Res8 = $"h3_res8_{lat:F4}_{lng:F4}";
            var h3Res5 = $"h3_res5_{lat:F2}_{lng:F2}";
            place.UpdateCoordinates(lat, lng, h3Res8, h3Res5);
        }

        if (request.Address is not null || request.City is not null || request.Country is not null || request.CountryCode is not null)
            place.SetAddress(request.Address, request.City, request.Country, request.CountryCode);

        if (request.Timezone is not null)
            place.SetTimezone(request.Timezone);

        if (request.IsPermanent.HasValue)
            place.SetIsPermanent(request.IsPermanent.Value);
    }

    private static void Validate(UpdatePlaceRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                errors["name"] = ["Name cannot be empty."];
            else if (request.Name.Length > 200)
                errors["name"] = ["Name must not exceed 200 characters."];
        }

        if (request.Description?.Length > 4000)
            errors["description"] = ["Description must not exceed 4000 characters."];

        if (request.Latitude.HasValue && (request.Latitude < -90m || request.Latitude > 90m))
            errors["latitude"] = ["Latitude must be between -90 and 90."];

        if (request.Longitude.HasValue && (request.Longitude < -180m || request.Longitude > 180m))
            errors["longitude"] = ["Longitude must be between -180 and 180."];

        if (request.CountryCode is not null && request.CountryCode.Length != 2)
            errors["countryCode"] = ["Country code must be exactly 2 characters."];

        if (request.Timezone is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Timezone))
                errors["timezone"] = ["Timezone cannot be empty."];
            else if (request.Timezone.Length > 100)
                errors["timezone"] = ["Timezone must not exceed 100 characters."];
        }

        if (request.CategoryIds is not null)
        {
            if (request.CategoryIds.Count == 0)
                errors["categoryIds"] = ["At least one category is required."];
            else if (request.CategoryIds.Count > 5)
                errors["categoryIds"] = ["A place cannot have more than 5 categories."];
        }

        if (request.ExternalLinks is not null)
        {
            for (var i = 0; i < request.ExternalLinks.Count; i++)
            {
                var link = request.ExternalLinks[i];
                if (string.IsNullOrWhiteSpace(link.Label) || link.Label.Length > 100)
                    errors[$"externalLinks[{i}].label"] = ["Label is required and must not exceed 100 characters."];
                if (string.IsNullOrWhiteSpace(link.Url) || link.Url.Length > 1024 ||
                    !Uri.IsWellFormedUriString(link.Url, UriKind.Absolute))
                    errors[$"externalLinks[{i}].url"] = ["A valid absolute URL is required, max 1024 characters."];
            }
        }

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }
}
