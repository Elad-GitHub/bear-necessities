using BearNecessities.Application.Services.Places;
using BearNecessities.Infrastructure.Places;
using FluentAssertions;

namespace BearNecessities.Infrastructure.Tests.Places;

public class GeoTimeZoneLookupTests
{
    private readonly GeoTimeZoneLookup _lookup = new();

    [Theory]
    [InlineData(31.62, 74.88, "Asia/Kolkata")]     // Amritsar
    [InlineData(38.72, -9.14, "Europe/Lisbon")]    // Lisbon
    [InlineData(39.74, 19.65, "Europe/Athens")]    // Corfu
    [InlineData(-8.51, 115.26, "Asia/Makassar")]   // Ubud, Bali
    [InlineData(20.21, -87.46, "America/Cancun")]  // Tulum
    public void FindRealZone_GivesTheZoneAtTheCoordinates(double latitude, double longitude, string expected) =>
        PlaceTimezoneCorrection.FindRealZone((decimal)latitude, (decimal)longitude, _lookup).Should().Be(expected);

    [Fact]
    public void FindRealZone_OpenSea_StaysUtc() =>
        PlaceTimezoneCorrection.FindRealZone(0m, -30m, _lookup).Should().BeNull();
}
