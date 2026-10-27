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

    private const double EarthRadiusKm = 6371.0;

    // Great-circle (haversine) distance — accurate enough for trip planning at any scale
    public double DistanceKmTo(Coordinates other)
    {
        var latitudeDelta = ToRadians((double)(other.Latitude - Latitude));
        var longitudeDelta = ToRadians((double)(other.Longitude - Longitude));
        var startLatitude = ToRadians((double)Latitude);
        var endLatitude = ToRadians((double)other.Latitude);

        var haversine = Math.Sin(latitudeDelta / 2) * Math.Sin(latitudeDelta / 2)
            + Math.Cos(startLatitude) * Math.Cos(endLatitude) * Math.Sin(longitudeDelta / 2) * Math.Sin(longitudeDelta / 2);
        return 2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(haversine)));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
