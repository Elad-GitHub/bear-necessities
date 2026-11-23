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

public class SmartItineraryPlannerTests
{
    private readonly SmartItineraryPlanner _planner = new();
    private readonly Category _yoga = Category.Create("yoga", "Yoga");
    private readonly Category _food = Category.Create("vegan-restaurant", "Vegan, Vegetarian & Raw");
    private readonly Category _dance = Category.Create("ecstatic-dance", "Ecstatic Dance");
    private readonly Category _party = Category.Create("dj-set", "DJ Set");

    private static Place PlaceAt(string name, decimal latitude, decimal longitude, params Category[] categories)
    {
        var place = Place.Create(name, latitude, longitude, "UTC", new UserId(Guid.NewGuid()));
        foreach (var category in categories) place.AddCategory(category);
        return place;
    }

    private ItineraryPlanningInput Input(IReadOnlyList<Place> candidates, int days, int stopsPerDay,
        Coordinates? start = null, string? wish = null, IReadOnlyList<Guid>? categoryIds = null) =>
        new(candidates, categoryIds ?? [_yoga.Id.Value, _food.Id.Value, _dance.Id.Value], start, days, stopsPerDay, wish);

    private static IReadOnlyList<IReadOnlyList<string>> Names(PlannedItinerary plan) =>
        plan.Days.Select(day => (IReadOnlyList<string>)day.Stops.Select(stop => stop.Place.Name).ToList()).ToList();

    [Fact]
    public void Plan_ShouldSplitStopsIntoDays()
    {
        var candidates = Enumerable.Range(0, 20)
            .Select(i => PlaceAt($"Place {i}", 38m + i * 0.01m, 23m, i % 2 == 0 ? _yoga : _food))
            .ToList();

        var plan = _planner.Plan(Input(candidates, days: 3, stopsPerDay: 4));

        plan.Days.Should().HaveCount(3);
        plan.Days.Should().OnlyContain(day => day.Stops.Count == 4);
        plan.Days.SelectMany(day => day.Stops).Select(stop => stop.Place).Should().OnlyHaveUniqueItems();
        plan.Planner.Should().Be("smart");
    }

    [Fact]
    public void Plan_FewerCandidatesThanSlots_ShouldUseAllAndShortenLastDay()
    {
        var candidates = Enumerable.Range(0, 5).Select(i => PlaceAt($"Place {i}", 38m + i * 0.01m, 23m, _yoga)).ToList();

        var plan = _planner.Plan(Input(candidates, days: 3, stopsPerDay: 4));

        plan.Days.Should().HaveCount(2);
        plan.Days[1].Stops.Should().HaveCount(1);
    }

    [Fact]
    public void Plan_ShouldRotateThroughInterests()
    {
        var start = Coordinates.Create(38m, 23m);
        // Yoga places are all closest, so pure nearest-neighbour would pick yoga three times in a row
        var candidates = new List<Place>
        {
            PlaceAt("Yoga A", 38.001m, 23m, _yoga),
            PlaceAt("Yoga B", 38.002m, 23m, _yoga),
            PlaceAt("Yoga C", 38.003m, 23m, _yoga),
            PlaceAt("Vegan", 38.05m, 23m, _food),
            PlaceAt("Dance", 38.06m, 23m, _dance)
        };

        var plan = _planner.Plan(Input(candidates, days: 1, stopsPerDay: 3, start));

        Names(plan)[0].Should().Equal("Yoga A", "Vegan", "Dance");
    }

    [Fact]
    public void Plan_ShouldStartNearTheStartPoint()
    {
        var start = Coordinates.Create(40m, 20m);
        var candidates = new List<Place> { PlaceAt("Far", 45m, 25m, _yoga), PlaceAt("Near", 40.01m, 20.01m, _yoga) };

        var plan = _planner.Plan(Input(candidates, days: 1, stopsPerDay: 2, start));

        Names(plan)[0][0].Should().Be("Near");
    }

    [Fact]
    public void Plan_NoCandidates_ShouldReturnNoDays()
    {
        _planner.Plan(Input([], days: 3, stopsPerDay: 4)).Days.Should().BeEmpty();
    }

    [Fact]
    public void Plan_WishAlone_ShouldPickTheSubcategoriesItNames()
    {
        var candidates = new List<Place>
        {
            PlaceAt("Shala", 38.001m, 23m, _yoga),
            PlaceAt("Club", 38.0005m, 23m, _party),
            PlaceAt("Green Bowl", 38.002m, 23m, _food)
        };

        var plan = _planner.Plan(Input(candidates, days: 1, stopsPerDay: 3, wish: "quiet days, vegan food", categoryIds: []));

        Names(plan)[0].Should().BeEquivalentTo("Shala", "Green Bowl");
    }

    [Fact]
    public void Plan_NegatedWish_ShouldLeaveThosePlacesOut()
    {
        var candidates = new List<Place>
        {
            PlaceAt("Shala", 38.001m, 23m, _yoga),
            PlaceAt("Club", 38.0005m, 23m, _party, _dance),
            PlaceAt("Dance Hall", 38.002m, 23m, _dance)
        };

        var plan = _planner.Plan(Input(candidates, days: 1, stopsPerDay: 3, wish: "dancing and yoga but no parties", categoryIds: []));

        Names(plan)[0].Should().NotContain("Club").And.Contain("Shala").And.Contain("Dance Hall");
    }

    [Fact]
    public void Plan_ShouldTitleTheDayAndExplainEachStop()
    {
        var shala = PlaceAt("Shala", 38.001m, 23m, _yoga);
        shala.SetAddress(null, "Ubud", "Indonesia", "ID");
        var plan = _planner.Plan(Input([shala], days: 1, stopsPerDay: 2));

        plan.Days[0].Title.Should().Be("Ubud · Yoga");
        plan.Days[0].Stops[0].Reason.Should().Be("For your interest in Yoga");
    }
}

public class PlanItineraryHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly PlanItineraryHandler _handler;
    private readonly List<Guid> _threeCategories = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

    public PlanItineraryHandlerTests()
    {
        _handler = new PlanItineraryHandler(_mockUnitOfWork.Object, new SmartItineraryPlanner());
    }

    private void GivenCountryPlaces(List<Place> places) =>
        Mock.Get(_mockUnitOfWork.Object.Places)
            .Setup(x => x.FindByCountryAsync("Greece", It.IsAny<CancellationToken>()))
            .ReturnsAsync(places);

    private static List<Place> YogaPlaces(Category category) => Enumerable.Range(0, 6).Select(i =>
    {
        var place = TestDataBuilder.BuildPlace(new UserId(Guid.NewGuid()), ListingStatus.Live);
        place.UpdateCoordinates(38m + i * 0.1m, 23m, "", "");
        place.AddCategory(category);
        return place;
    }).ToList();

    [Fact]
    public async Task HandleAsync_ShouldReturnNumberedDaysWithDistancesAndReasons()
    {
        var category = Category.Create("yoga", "Yoga");
        GivenCountryPlaces(YogaPlaces(category));
        var categories = new List<Guid> { category.Id.Value, Guid.NewGuid(), Guid.NewGuid() };

        var response = await _handler.HandleAsync(new PlanItineraryRequest("Greece", null, null, categories, Days: 2, StopsPerDay: 3));

        response.Days.Select(day => day.DayNumber).Should().Equal(1, 2);
        response.TotalStops.Should().Be(6);
        response.Planner.Should().Be("smart");
        response.Days[0].DistanceKm.Should().BeGreaterThan(0);
        response.Days[0].Reasons.Should().HaveCount(3);
    }

    [Fact]
    public async Task HandleAsync_WishWithoutCategories_ShouldPlan()
    {
        GivenCountryPlaces(YogaPlaces(Category.Create("yoga", "Yoga")));

        var response = await _handler.HandleAsync(new PlanItineraryRequest("Greece", null, null, [], Days: 1, StopsPerDay: 2, Wish: "yoga"));

        response.TotalStops.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_FewerThanThreeCategoriesAndNoWish_ShouldThrowValidation()
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

    [Fact]
    public async Task HandleAsync_TooLongWish_ShouldThrowValidation()
    {
        var action = () => _handler.HandleAsync(
            new PlanItineraryRequest("Greece", null, null, [], Wish: new string('a', PlanItineraryHandler.MaxWishLength + 1)));

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
