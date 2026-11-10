using Moq;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.Services.Trips;
using BearNecessities.Application.Tests.Helpers;
using BearNecessities.Application.UseCases.Trips.PlanItinerary;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Tests.UseCases.Trips;

public class NearestNeighborItineraryPlannerTests
{
    private readonly NearestNeighborItineraryPlanner _planner = new();
    private readonly Category _yoga = Category.Create("yoga", "Yoga");
    private readonly Category _food = Category.Create("vegan", "Vegan Food");
    private readonly Category _music = Category.Create("ecstatic-dance", "Ecstatic Dance");

    private static Place PlaceAt(string name, decimal latitude, decimal longitude, Category category)
    {
        var place = Place.Create(name, latitude, longitude, "UTC", new UserId(Guid.NewGuid()));
        place.AddCategory(category);
        return place;
    }

    private ItineraryPlanningInput Input(IReadOnlyList<Place> candidates, int days, int stopsPerDay, Coordinates? start = null) =>
        new(candidates, [_yoga.Id.Value, _food.Id.Value, _music.Id.Value], start, days, stopsPerDay);

    [Fact]
    public async Task PlanAsync_ShouldSplitStopsIntoDays()
    {
        var candidates = Enumerable.Range(0, 20)
            .Select(i => PlaceAt($"Place {i}", 38m + i * 0.01m, 23m, i % 2 == 0 ? _yoga : _food))
            .ToList();

        var days = await _planner.PlanAsync(Input(candidates, days: 3, stopsPerDay: 4));

        days.Should().HaveCount(3);
        days.Should().OnlyContain(day => day.Count == 4);
        days.SelectMany(day => day).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task PlanAsync_FewerCandidatesThanSlots_ShouldUseAllAndShortenLastDay()
    {
        var candidates = Enumerable.Range(0, 5).Select(i => PlaceAt($"Place {i}", 38m + i * 0.01m, 23m, _yoga)).ToList();

        var days = await _planner.PlanAsync(Input(candidates, days: 3, stopsPerDay: 4));

        days.Should().HaveCount(2);
        days[1].Should().HaveCount(1);
    }

    [Fact]
    public async Task PlanAsync_ShouldRotateThroughCategories()
    {
        var start = Coordinates.Create(38m, 23m);
        // Yoga places are all closest, so pure nearest-neighbour would pick yoga three times in a row
        var candidates = new List<Place>
        {
            PlaceAt("Yoga A", 38.001m, 23m, _yoga),
            PlaceAt("Yoga B", 38.002m, 23m, _yoga),
            PlaceAt("Yoga C", 38.003m, 23m, _yoga),
            PlaceAt("Vegan", 38.05m, 23m, _food),
            PlaceAt("Dance", 38.06m, 23m, _music)
        };

        var days = await _planner.PlanAsync(Input(candidates, days: 1, stopsPerDay: 3, start));

        days[0].Select(place => place.Name).Should().Equal("Yoga A", "Vegan", "Dance");
    }

    [Fact]
    public async Task PlanAsync_ShouldStartNearTheStartPoint()
    {
        var start = Coordinates.Create(40m, 20m);
        var candidates = new List<Place>
        {
            PlaceAt("Far", 45m, 25m, _yoga),
            PlaceAt("Near", 40.01m, 20.01m, _yoga)
        };

        var days = await _planner.PlanAsync(Input(candidates, days: 1, stopsPerDay: 2, start));

        days[0][0].Name.Should().Be("Near");
    }

    [Fact]
    public async Task PlanAsync_NoCandidates_ShouldReturnNoDays()
    {
        var days = await _planner.PlanAsync(Input([], days: 3, stopsPerDay: 4));

        days.Should().BeEmpty();
    }
}

public class PlanItineraryHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly PlanItineraryHandler _handler;
    private readonly List<Guid> _threeCategories = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

    public PlanItineraryHandlerTests()
    {
        _handler = new PlanItineraryHandler(_mockUnitOfWork.Object, new NearestNeighborItineraryPlanner());
    }

    private void GivenCountryPlaces(List<Place> places) =>
        Mock.Get(_mockUnitOfWork.Object.Places)
            .Setup(x => x.FindByCountryAsync("Greece", It.IsAny<CancellationToken>()))
            .ReturnsAsync(places);

    [Fact]
    public async Task HandleAsync_ShouldReturnNumberedDaysWithDistances()
    {
        var category = Category.Create("yoga", "Yoga");
        var places = Enumerable.Range(0, 6).Select(i =>
        {
            var place = TestDataBuilder.BuildPlace(new UserId(Guid.NewGuid()), ListingStatus.Live);
            place.UpdateCoordinates(38m + i * 0.1m, 23m, "", "");
            place.AddCategory(category);
            return place;
        }).ToList();
        GivenCountryPlaces(places);
        var categories = new List<Guid> { category.Id.Value, Guid.NewGuid(), Guid.NewGuid() };

        var response = await _handler.HandleAsync(new PlanItineraryRequest("Greece", null, null, categories, Days: 2, StopsPerDay: 3));

        response.Days.Select(day => day.DayNumber).Should().Equal(1, 2);
        response.TotalStops.Should().Be(6);
        response.Planner.Should().Be("nearest-neighbor");
        response.Days[0].DistanceKm.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task HandleAsync_FewerThanThreeCategories_ShouldThrowValidation()
    {
        var action = () => _handler.HandleAsync(
            new PlanItineraryRequest("Greece", null, null, [Guid.NewGuid(), Guid.NewGuid()]));

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_DuplicateCategoryIds_ShouldNotCountTwice()
    {
        var repeated = Guid.NewGuid();

        var action = () => _handler.HandleAsync(
            new PlanItineraryRequest("Greece", null, null, [repeated, repeated, Guid.NewGuid()]));

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(15, 4)]
    [InlineData(3, 0)]
    [InlineData(3, 9)]
    public async Task HandleAsync_OutOfRangeDaysOrStops_ShouldThrowValidation(int days, int stopsPerDay)
    {
        var action = () => _handler.HandleAsync(
            new PlanItineraryRequest("Greece", null, null, _threeCategories, days, stopsPerDay));

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_NothingMatches_ShouldReturnEmptyItinerary()
    {
        GivenCountryPlaces([]);

        var response = await _handler.HandleAsync(new PlanItineraryRequest("Greece", null, null, _threeCategories));

        response.Days.Should().BeEmpty();
        response.TotalStops.Should().Be(0);
    }
}
