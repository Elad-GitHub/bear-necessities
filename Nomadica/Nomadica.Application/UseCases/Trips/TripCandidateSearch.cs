using Nomadica.Application.DTOs.Places;
using Nomadica.Application.Exceptions;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Trips;

/// <summary>Finds the places a trip can draw from: a whole country or a radius around a point, optionally filtered by category.</summary>
public static class TripCandidateSearch
{
    public const double MaxRadiusKm = 500;

    public static async Task<List<Place>> FindAsync(
        IUnitOfWork unitOfWork,
        string? country,
        decimal? latitude,
        decimal? longitude,
        double radiusKm,
        IReadOnlyCollection<Guid>? categoryIds,
        CancellationToken cancellationToken)
    {
        var places = await FindInAreaAsync(unitOfWork, country, latitude, longitude, radiusKm, cancellationToken);
        if (categoryIds is not { Count: > 0 })
            return places;

        var categoryFilter = categoryIds.ToHashSet();
        return places
            .Where(place => place.Categories.Any(category => categoryFilter.Contains(category.Id.Value)))
            .ToList();
    }

    public static PlaceSummaryDto ToSummary(Place place) => new(
        Id: place.Id.Value,
        Name: place.Name,
        Latitude: place.Latitude,
        Longitude: place.Longitude,
        H3IndexRes8: place.H3IndexRes8,
        City: place.City,
        Country: place.Country,
        Status: place.Status,
        AverageRating: null,
        ReviewCount: 0,
        CategoryIds: place.Categories.Select(category => category.Id.Value).ToList(),
        ThumbnailUrl: null,
        IsPermanent: place.IsPermanent);

    private static async Task<List<Place>> FindInAreaAsync(
        IUnitOfWork unitOfWork,
        string? country,
        decimal? latitude,
        decimal? longitude,
        double radiusKm,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(country))
            return await unitOfWork.Places.FindByCountryAsync(country, cancellationToken);

        if (latitude.HasValue && longitude.HasValue)
        {
            var radius = Math.Clamp(radiusKm, 1, MaxRadiusKm);
            var center = Coordinates.Create(latitude.Value, longitude.Value);
            var nearby = await unitOfWork.Places.FindNearbyAsync(center, radius, cancellationToken);
            return nearby.ToList();
        }

        throw new ValidationException(new Dictionary<string, string[]>
        {
            ["area"] = ["Provide either a country or a latitude/longitude center."]
        });
    }
}
