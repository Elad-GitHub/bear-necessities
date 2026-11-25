using BearNecessities.Application.Interfaces;
using BearNecessities.Application.Services.Places;
using FluentAssertions;
using Moq;

namespace BearNecessities.Application.Tests.Services;

public class PlaceTimezoneCorrectionTests
{
    [Theory]
    [InlineData("UTC", true)]
    [InlineData("utc", true)]
    [InlineData("Etc/UTC", true)]
    [InlineData("", true)]
    [InlineData(null, true)]
    [InlineData("Asia/Kolkata", false)]
    [InlineData("Europe/Lisbon", false)]
    public void NeedsCorrection_OnlyForPlaceholderZones(string? timezone, bool expected) =>
        PlaceTimezoneCorrection.NeedsCorrection(timezone).Should().Be(expected);

    [Fact]
    public void FindRealZone_ReturnsTheLandZone()
    {
        var lookup = LookupReturning("Asia/Kolkata");

        PlaceTimezoneCorrection.FindRealZone(31.62m, 74.88m, lookup).Should().Be("Asia/Kolkata");
    }

    [Theory]
    [InlineData("Etc/GMT-4")]
    [InlineData("Etc/UTC")]
    [InlineData("")]
    public void FindRealZone_KeepsUtcWhenOnlyASeaOffsetIsKnown(string zone)
    {
        var lookup = LookupReturning(zone);

        PlaceTimezoneCorrection.FindRealZone(10m, 60m, lookup).Should().BeNull();
    }

    private static ITimeZoneLookup LookupReturning(string zone)
    {
        var lookup = new Mock<ITimeZoneLookup>();
        lookup.Setup(x => x.FindTimeZone(It.IsAny<decimal>(), It.IsAny<decimal>())).Returns(zone);
        return lookup.Object;
    }
}
