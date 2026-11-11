using System.Text.Json;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Places.CreatePlace;

public sealed class CreatePlaceHandler : IRequestHandler<CreatePlaceRequest, CreatePlaceResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreatePlaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<CreatePlaceResponse> HandleAsync(CreatePlaceRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        Validate(request);

        var categoryIds = request.CategoryIds.Select(id => new CategoryId(id)).ToList();
        var foundCategories = new List<Category>();

        foreach (var categoryId in categoryIds)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken);
            if (category is null)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["categoryIds"] = [$"Category not found: {categoryId.Value}."]
                });
            }
            foundCategories.Add(category);
        }

        var h3Res8 = $"h3_res8_{request.Latitude:F4}_{request.Longitude:F4}";
        var h3Res5 = $"h3_res5_{request.Latitude:F2}_{request.Longitude:F2}";

        var place = Place.Create(request.Name, request.Latitude, request.Longitude, request.Timezone, _currentUser.UserId);
        place.SetDescription(request.Description);
        place.SetAddress(request.Address, request.City, request.Country, request.CountryCode);
        place.SetIsPermanent(request.IsPermanent);
        place.UpdateCoordinates(request.Latitude, request.Longitude, h3Res8, h3Res5);
        place.Submit();
        // Admins are the reviewers, so their own listings skip the queue
        if (_currentUser.HasRole(UserRole.Admin)) place.Approve(_currentUser.UserId);

        foreach (var category in foundCategories)
            place.AddCategory(category);

        var sortOrder = 0;
        foreach (var linkDto in request.ExternalLinks ?? [])
            place.AddExternalLink(PlaceExternalLink.Create(place.Id, linkDto.Label, linkDto.Url, sortOrder++));

        await _unitOfWork.Places.AddAsync(place, cancellationToken);

        if (_unitOfWork.ChangeLogs is not null)
        {
            var changeLog = ListingChangeLog.Create(
                ListingChangeTargetType.Place,
                place.Id.Value,
                _currentUser.UserId,
                ChangeType.StatusChange,
                newData: JsonSerializer.Serialize(new { place.Name, place.Latitude, place.Longitude, Status = place.Status.ToString() }));
            await _unitOfWork.ChangeLogs.AddAsync(changeLog, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreatePlaceResponse(place.Id.Value, place.Status.ToString());
    }

    private static void Validate(CreatePlaceRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors["name"] = ["Name is required."];
        else if (request.Name.Length > 200)
            errors["name"] = ["Name must not exceed 200 characters."];

        if (request.Description?.Length > 4000)
            errors["description"] = ["Description must not exceed 4000 characters."];

        if (request.Latitude < -90m || request.Latitude > 90m)
            errors["latitude"] = ["Latitude must be between -90 and 90."];

        if (request.Longitude < -180m || request.Longitude > 180m)
            errors["longitude"] = ["Longitude must be between -180 and 180."];

        if (request.Address?.Length > 500)
            errors["address"] = ["Address must not exceed 500 characters."];

        if (request.City?.Length > 100)
            errors["city"] = ["City must not exceed 100 characters."];

        if (request.Country?.Length > 100)
            errors["country"] = ["Country must not exceed 100 characters."];

        if (request.CountryCode is not null && request.CountryCode.Length != 2)
            errors["countryCode"] = ["Country code must be exactly 2 characters."];

        if (string.IsNullOrWhiteSpace(request.Timezone))
            errors["timezone"] = ["Timezone is required."];
        else if (request.Timezone.Length > 100)
            errors["timezone"] = ["Timezone must not exceed 100 characters."];

        if (request.CategoryIds is null || request.CategoryIds.Count == 0)
            errors["categoryIds"] = ["At least one category is required."];
        else if (request.CategoryIds.Count > 5)
            errors["categoryIds"] = ["A place cannot have more than 5 categories."];

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
