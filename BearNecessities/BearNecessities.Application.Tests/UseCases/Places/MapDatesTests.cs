using BearNecessities.Application.UseCases.Places;

namespace BearNecessities.Application.Tests.UseCases.Places;

public class MapDatesTests
{
    [Fact]
    public void ToRange_NoDates_ShouldMeanAnytime()
    {
        MapDates.ToRange(null, null).Should().BeNull();
    }

    [Fact]
    public void ToRange_SingleDay_ShouldCoverTheWholeDay()
    {
        var range = MapDates.ToRange(new DateOnly(2027, 7, 20), null)!;

        range.From.Should().Be(new DateTime(2027, 7, 20, 0, 0, 0, DateTimeKind.Utc));
        range.To.Date.Should().Be(new DateTime(2027, 7, 20));
        range.To.Hour.Should().Be(23);
    }

    [Fact]
    public void ToRange_DatesInReverse_ShouldBeSwapped()
    {
        var range = MapDates.ToRange(new DateOnly(2027, 7, 25), new DateOnly(2027, 7, 18))!;

        range.From.Date.Should().Be(new DateTime(2027, 7, 18));
        range.To.Date.Should().Be(new DateTime(2027, 7, 25));
    }
}
