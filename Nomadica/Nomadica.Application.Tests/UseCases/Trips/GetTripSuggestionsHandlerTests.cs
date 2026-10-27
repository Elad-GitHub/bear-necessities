using Moq;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Tests.Helpers;
using Nomadica.Application.UseCases.Trips.GetTripSuggestions;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.Tests.UseCases.Trips;

public class GetTripSuggestionsHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly GetTripSuggestionsHandler _handler;

    public GetTripSuggestionsHandlerTests()
    {
        Mock.Get(_mockUnitOfWork.Object.Events)
            .Setup(x => x.FindUpcomingByPlacesAsync(It.IsAny<IEnumerable<PlaceId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Event>());
        _handler = new GetTripSuggestionsHandler(_mockUnitOfWork.Object);
    }

    private Mock<IPlaceRepository> PlaceRepository => Mock.Get(_mockUnitOfWork.Object.Places);

    private static Place BuildPlaceIn(Category category)
    {
        var place = TestDataBuilder.BuildPlace(new UserId(Guid.NewGuid()), ListingStatus.Live);
        place.AddCategory(category);
        return place;
    }

    [Fact]
    public async Task HandleAsync_ByCountry_ShouldSearchCountry()
    {
        var place = BuildPlaceIn(Category.Create("yoga", "Yoga"));
        PlaceRepository.Setup(x => x.FindByCountryAsync("Greece", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Place> { place });

        var response = await _handler.HandleAsync(new GetTripSuggestionsRequest("Greece", null, null));

        response.Places.Should().ContainSingle().Which.Id.Should().Be(place.Id.Value);
        PlaceRepository.Verify(x => x.FindNearbyAsync(It.IsAny<Coordinates>(), It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ByCoordinates_ShouldClampRadiusTo500Km()
    {
        PlaceRepository.Setup(x => x.FindNearbyAsync(It.IsAny<Coordinates>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Place>());

        await _handler.HandleAsync(new GetTripSuggestionsRequest(null, 38.0m, 23.7m, RadiusKm: 5000));

        PlaceRepository.Verify(x => x.FindNearbyAsync(It.IsAny<Coordinates>(), 500, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithCategoryFilter_ShouldKeepOnlyMatchingPlaces()
    {
        var yoga = Category.Create("yoga", "Yoga");
        var market = Category.Create("market", "Market");
        var yogaPlace = BuildPlaceIn(yoga);
        PlaceRepository.Setup(x => x.FindByCountryAsync("Greece", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Place> { yogaPlace, BuildPlaceIn(market) });

        var response = await _handler.HandleAsync(
            new GetTripSuggestionsRequest("Greece", null, null, CategoryIds: new List<Guid> { yoga.Id.Value }));

        response.Places.Should().ContainSingle().Which.Id.Should().Be(yogaPlace.Id.Value);
    }

    [Fact]
    public async Task HandleAsync_NoCountryOrCenter_ShouldThrowValidation()
    {
        var action = () => _handler.HandleAsync(new GetTripSuggestionsRequest(null, 38.0m, null));

        await action.Should().ThrowAsync<ValidationException>();
    }
}
