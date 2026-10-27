using Moq;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Application.Tests.Helpers;
using Nomadica.Application.UseCases.Places.UpdatePlace;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.Tests.UseCases.Places;

public class UpdatePlaceHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly Mock<ICurrentUserService> _mockCurrentUser = new();
    private readonly UserId _ownerId = new(Guid.NewGuid());
    private readonly UpdatePlaceHandler _handler;

    public UpdatePlaceHandlerTests()
    {
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.UserId).Returns(_ownerId);
        _mockCurrentUser.Setup(x => x.HasRole(UserRole.Admin)).Returns(false);
        _handler = new UpdatePlaceHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object);
    }

    private Place GivenPlace(ListingStatus status, UserId? ownerId = null)
    {
        var place = TestDataBuilder.BuildPlace(ownerId ?? _ownerId, status);
        Mock.Get(_mockUnitOfWork.Object.Places)
            .Setup(x => x.GetByIdWithRelationsAsync(It.IsAny<PlaceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(place);
        return place;
    }

    private static UpdatePlaceRequest Request(Guid placeId, string? name = null, string? description = null,
        decimal? latitude = null, decimal? longitude = null, string? countryCode = null, List<Guid>? categoryIds = null) =>
        new(placeId, name, description, latitude, longitude, null, null, null, countryCode, null, null, categoryIds, null);

    [Fact]
    public async Task HandleAsync_MinorEditOnLivePlace_ShouldStayLive()
    {
        var place = GivenPlace(ListingStatus.Live);

        var response = await _handler.HandleAsync(Request(place.Id.Value, description: "New description"));

        response.Status.Should().Be("Live");
        response.RequiresReview.Should().BeFalse();
        place.Description.Should().Be("New description");
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_RenameLivePlace_ShouldSendBackToReview()
    {
        var place = GivenPlace(ListingStatus.Live);

        var response = await _handler.HandleAsync(Request(place.Id.Value, name: "Renamed Place"));

        response.RequiresReview.Should().BeTrue();
        response.Status.Should().Be("PendingReview");
        place.Name.Should().Be("Renamed Place");
    }

    [Fact]
    public async Task HandleAsync_MoveLivePlace_ShouldSendBackToReview()
    {
        var place = GivenPlace(ListingStatus.Live);

        var response = await _handler.HandleAsync(Request(place.Id.Value, latitude: 39.0m, longitude: 22.0m));

        response.RequiresReview.Should().BeTrue();
        place.Latitude.Should().Be(39.0m);
        place.Longitude.Should().Be(22.0m);
    }

    [Fact]
    public async Task HandleAsync_SameNameOnLivePlace_ShouldNotRequireReview()
    {
        var place = GivenPlace(ListingStatus.Live);

        var response = await _handler.HandleAsync(Request(place.Id.Value, name: place.Name));

        response.RequiresReview.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_RenamePendingPlace_ShouldNotFlagReviewAgain()
    {
        var place = GivenPlace(ListingStatus.PendingReview);

        var response = await _handler.HandleAsync(Request(place.Id.Value, name: "Renamed Place"));

        response.RequiresReview.Should().BeFalse();
        response.Status.Should().Be("PendingReview");
    }

    [Fact]
    public async Task HandleAsync_ByStranger_ShouldThrowForbidden()
    {
        var place = GivenPlace(ListingStatus.Live, new UserId(Guid.NewGuid()));

        var action = () => _handler.HandleAsync(Request(place.Id.Value, description: "x"));

        await action.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task HandleAsync_AdminEditingOthersPlace_ShouldSucceed()
    {
        _mockCurrentUser.Setup(x => x.HasRole(UserRole.Admin)).Returns(true);
        var place = GivenPlace(ListingStatus.Live, new UserId(Guid.NewGuid()));

        var response = await _handler.HandleAsync(Request(place.Id.Value, description: "Admin fix"));

        response.Status.Should().Be("Live");
    }

    [Theory]
    [InlineData(91, null)]
    [InlineData(null, -181)]
    public async Task HandleAsync_InvalidCoordinates_ShouldThrowValidation(int? latitude, int? longitude)
    {
        var place = GivenPlace(ListingStatus.Live);

        var action = () => _handler.HandleAsync(Request(place.Id.Value, latitude: latitude, longitude: longitude));

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_BlankName_ShouldThrowValidation()
    {
        var place = GivenPlace(ListingStatus.Live);

        var action = () => _handler.HandleAsync(Request(place.Id.Value, name: "   "));

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_ThreeLetterCountryCode_ShouldThrowValidation()
    {
        var place = GivenPlace(ListingStatus.Live);

        var action = () => _handler.HandleAsync(Request(place.Id.Value, countryCode: "GRC"));

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_EmptyCategoryList_ShouldThrowValidation()
    {
        var place = GivenPlace(ListingStatus.Live);

        var action = () => _handler.HandleAsync(Request(place.Id.Value, categoryIds: new List<Guid>()));

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_UnknownCategory_ShouldThrowValidation()
    {
        var place = GivenPlace(ListingStatus.Live);
        Mock.Get(_mockUnitOfWork.Object.Categories)
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        var action = () => _handler.HandleAsync(Request(place.Id.Value, categoryIds: new List<Guid> { Guid.NewGuid() }));

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_NewCategories_ShouldReplaceAndRequireReview()
    {
        var place = GivenPlace(ListingStatus.Live);
        var category = Category.Create("yoga", "Yoga");
        Mock.Get(_mockUnitOfWork.Object.Categories)
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { category });

        var response = await _handler.HandleAsync(Request(place.Id.Value, categoryIds: new List<Guid> { category.Id.Value }));

        place.Categories.Should().ContainSingle().Which.Should().Be(category);
        response.RequiresReview.Should().BeTrue();
    }
}
