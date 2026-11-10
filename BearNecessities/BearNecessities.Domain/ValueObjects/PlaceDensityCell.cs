namespace BearNecessities.Domain.ValueObjects;

/// <summary>
/// One grid cell of live places: where they sit on average, how many there are, and the box they span
/// (so a click can zoom straight to where the places actually are).
/// </summary>
public sealed record PlaceDensityCell(
    decimal Latitude,
    decimal Longitude,
    int Count,
    decimal MinLatitude,
    decimal MinLongitude,
    decimal MaxLatitude,
    decimal MaxLongitude);
