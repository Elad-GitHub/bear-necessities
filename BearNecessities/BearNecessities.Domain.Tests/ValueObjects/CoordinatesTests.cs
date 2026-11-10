using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Tests.ValueObjects;

public class CoordinatesTests
{
    [Fact]
    public void DistanceKmTo_AthensToThessaloniki_ShouldBeAbout300Km()
    {
        var athens = Coordinates.Create(37.9838m, 23.7275m);
        var thessaloniki = Coordinates.Create(40.6401m, 22.9444m);

        athens.DistanceKmTo(thessaloniki).Should().BeApproximately(302, 3);
    }

    [Fact]
    public void DistanceKmTo_SamePoint_ShouldBeZero()
    {
        var point = Coordinates.Create(9.93m, -84.08m);

        point.DistanceKmTo(point).Should().Be(0);
    }

    [Fact]
    public void DistanceKmTo_ShouldBeSymmetric()
    {
        var lisbon = Coordinates.Create(38.72m, -9.14m);
        var bangkok = Coordinates.Create(13.75m, 100.50m);

        lisbon.DistanceKmTo(bangkok).Should().BeApproximately(bangkok.DistanceKmTo(lisbon), 0.001);
    }
}
