namespace Nomadica.Domain.ValueObjects;

/// <summary>One grid cell of live places: where they sit on average and how many there are.</summary>
public sealed record PlaceDensityCell(decimal Latitude, decimal Longitude, int Count);
