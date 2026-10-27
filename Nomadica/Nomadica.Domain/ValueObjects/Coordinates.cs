namespace Nomadica.Domain.ValueObjects;

public sealed record Coordinates
{
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }

    private Coordinates() { }

    public static Coordinates Create(decimal latitude, decimal longitude)
    {
        if (latitude < -90m || latitude > 90m)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        if (longitude < -180m || longitude > 180m)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");

        return new Coordinates { Latitude = latitude, Longitude = longitude };
    }
}
