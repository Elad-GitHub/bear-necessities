using Nomadica.Domain.Enums;

namespace Nomadica.Application.Imports;

/// A normalized, source-agnostic place record fed into the import pipeline.
public sealed record ImportPlace(
    string Name,
    decimal Latitude,
    decimal Longitude,
    IReadOnlyList<string> CategorySlugs,
    ListingSourceType Source,
    string? ExternalSourceId = null,
    string? Description = null,
    string? Address = null,
    string? City = null,
    string? Country = null,
    string? CountryCode = null,
    string? Website = null,
    bool IsPermanent = true);
