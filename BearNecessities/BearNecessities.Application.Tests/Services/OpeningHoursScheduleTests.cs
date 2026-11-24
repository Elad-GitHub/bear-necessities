using BearNecessities.Application.Services.Places;

namespace BearNecessities.Application.Tests.Services;

public class OpeningHoursScheduleTests
{
    // 2026-11-23 is a Monday
    private static readonly DateTime Monday = new(2026, 11, 23);

    private static OpenState StateAt(string hours, DateTime localTime) =>
        OpeningHoursSchedule.Parse(hours)!.StateAt(localTime);

    [Fact]
    public void WeekdayRange_ShouldBeOpenInsideAndClosedOutside()
    {
        StateAt("Mo-Fr 09:00-18:00", Monday.AddHours(10)).Should().Be(new OpenState(true, Monday.AddHours(18)));
        StateAt("Mo-Fr 09:00-18:00", Monday.AddHours(18)).IsOpen.Should().BeFalse();
        StateAt("Mo-Fr 09:00-18:00", Monday.AddDays(5).AddHours(10)).IsOpen.Should().BeFalse();
    }

    [Fact]
    public void SplitDay_ShouldBeClosedAtLunch()
    {
        const string hours = "Mo-Su 12:00-15:00,19:00-23:00";
        StateAt(hours, Monday.AddHours(16)).IsOpen.Should().BeFalse();
        StateAt(hours, Monday.AddHours(20)).ClosesAt.Should().Be(Monday.AddHours(23));
    }

    [Fact]
    public void RangePastMidnight_ShouldStayOpenIntoTheNextDay()
    {
        const string hours = "Mo-Sa 18:00-01:00, Su 14:30-24:00";
        var tuesdayHalfPastMidnight = Monday.AddDays(1).AddMinutes(30);
        StateAt(hours, tuesdayHalfPastMidnight).Should().Be(new OpenState(true, Monday.AddDays(1).AddHours(1)));
        StateAt(hours, Monday.AddDays(6).AddHours(15)).IsOpen.Should().BeTrue();
    }

    [Fact]
    public void LaterRule_ShouldReplaceEarlierForThatDay()
    {
        StateAt("Mo-Su 10:00-20:00; Mo off", Monday.AddHours(12)).IsOpen.Should().BeFalse();
        StateAt("Mo-Su 10:00-20:00; Mo off", Monday.AddDays(1).AddHours(12)).IsOpen.Should().BeTrue();
    }

    [Fact]
    public void MonthRules_ShouldPickTheSeason()
    {
        const string hours = "May-Oct 09:00-21:00; Nov-Apr 09:00-19:00";
        StateAt(hours, Monday.AddHours(20)).IsOpen.Should().BeFalse();
        StateAt(hours, new DateTime(2027, 6, 1, 20, 0, 0)).IsOpen.Should().BeTrue();
    }

    [Fact]
    public void AlwaysOpen_ShouldNeverClose()
    {
        StateAt("24/7", Monday.AddHours(3)).Should().Be(new OpenState(true, null));
    }

    [Fact]
    public void DayList_ShouldSelectOnlyThoseDays()
    {
        StateAt("We, Sa 08:00-14:00", Monday.AddDays(2).AddHours(9)).IsOpen.Should().BeTrue();
        StateAt("We, Sa 08:00-14:00", Monday.AddHours(9)).IsOpen.Should().BeFalse();
    }

    [Fact]
    public void HolidayRules_ShouldBeIgnored()
    {
        StateAt("Mo-Fr 09:00-17:00; PH off", Monday.AddHours(10)).IsOpen.Should().BeTrue();
    }

    [Theory]
    [InlineData("14:00 -")]
    [InlineData("fechado Definitivamente")]
    [InlineData("Mo-Fr 09:00-18:00; by appointment")]
    [InlineData("")]
    public void UnreadableHours_ShouldBeUnknown(string hours)
    {
        OpeningHoursSchedule.Parse(hours).Should().BeNull();
    }
}
