using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.UseCases.Places.GetHappeningNow;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Tests.UseCases.Places;

public class GetHappeningNowHandlerTests
{
    // Monday 2026-11-23, 10:00 UTC; the zone lookup below keeps local time equal to UTC
    private static readonly DateTime NowUtc = new(2026, 11, 23, 10, 0, 0, DateTimeKind.Utc);
    private const decimal CenterLatitude = -8.5m;
    private const decimal CenterLongitude = 115.26m;

    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly Mock<ITimeZoneLookup> _mockTimeZones = new();
    private readonly UserId _ownerId = new(Guid.NewGuid());

    public GetHappeningNowHandlerTests()
    {
        _mockTimeZones.Setup(x => x.ToLocalTime(It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<decimal>()))
            .Returns((DateTime utc, decimal _, decimal _) => utc);
        Mock.Get(_mockUnitOfWork.Object.Events)
            .Setup(x => x.FindNextEventByPlacesAsync(It.IsAny<IEnumerable<PlaceId>>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<PlaceId, PlaceNextEvent>());
    }

    private GetHappeningNowHandler Handler => new(_mockUnitOfWork.Object, _mockTimeZones.Object, new FixedClock(NowUtc));

    private Place PlaceAt(string name, decimal latitudeOffset, string? hours)
    {
        var place = Place.Create(name, CenterLatitude + latitudeOffset, CenterLongitude, "UTC", _ownerId);
        place.SetOpeningHours(hours);
        return place;
    }

    private void GivenNearby(params Place[] places) =>
        Mock.Get(_mockUnitOfWork.Object.Places)
            .Setup(x => x.FindNearbyAsync(It.IsAny<Coordinates>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(places);

    [Fact]
    public async Task ShouldListOnlyPlacesOpenNow_ClosestFirst_WithClosingTime()
    {
        var farCafe = PlaceAt("Far café", 0.05m, "Mo-Su 08:00-20:00");
        var nearCafe = PlaceAt("Near café", 0.01m, "Mo-Fr 09:00-18:00");
        var eveningBar = PlaceAt("Evening bar", 0.01m, "Mo-Su 18:00-23:00");
        var noHours = PlaceAt("Retreat", 0.01m, null);
        GivenNearby(farCafe, nearCafe, eveningBar, noHours);

        var response = await Handler.HandleAsync(new GetHappeningNowRequest(CenterLatitude, CenterLongitude));

        response.OpenPlaces.Select(place => place.Name).Should().Equal("Near café", "Far café");
        response.OpenPlaces[0].ClosesAt.Should().Be("18:00");
        response.PlacesNearby.Should().Be(4);
        response.PlacesWithHours.Should().Be(3);
    }

    [Fact]
    public async Task ShouldDropPlacesOutsideTheRadius()
    {
        GivenNearby(PlaceAt("Corner of the box", 0.3m, "24/7"));

        var response = await Handler.HandleAsync(new GetHappeningNowRequest(CenterLatitude, CenterLongitude, RadiusKm: 25));

        response.OpenPlaces.Should().BeEmpty();
        response.PlacesNearby.Should().Be(0);
    }

    [Fact]
    public async Task ShouldListDatedEventsSoonestFirst_AndMarkTheOngoingOne()
    {
        var studio = PlaceAt("Studio", 0.01m, null);
        var garden = PlaceAt("Garden", 0.02m, null);
        GivenNearby(studio, garden);
        Mock.Get(_mockUnitOfWork.Object.Events)
            .Setup(x => x.FindNextEventByPlacesAsync(It.IsAny<IEnumerable<PlaceId>>(), NowUtc, NowUtc + GetHappeningNowHandler.UpcomingWindow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<PlaceId, PlaceNextEvent>
            {
                [studio.Id] = new(new EventId(Guid.NewGuid()), "Evening kirtan", NowUtc.AddHours(8)),
                [garden.Id] = new(new EventId(Guid.NewGuid()), "Morning dance", NowUtc.AddHours(-1))
            });

        var response = await Handler.HandleAsync(new GetHappeningNowRequest(CenterLatitude, CenterLongitude));

        response.Events.Select(happening => happening.Title).Should().Equal("Morning dance", "Evening kirtan");
        response.Events[0].IsOngoing.Should().BeTrue();
        response.Events[1].IsOngoing.Should().BeFalse();
    }

    [Fact]
    public async Task BadLocation_ShouldThrowValidation()
    {
        var action = () => Handler.HandleAsync(new GetHappeningNowRequest(120m, 0m));

        await action.Should().ThrowAsync<ValidationException>();
    }

    private sealed class FixedClock(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
