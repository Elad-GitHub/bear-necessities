using Moq;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.Tests.Helpers;
using BearNecessities.Application.UseCases.Places.ApprovePlace;
using BearNecessities.Application.UseCases.Places.DeletePlace;
using BearNecessities.Application.UseCases.Places.RejectPlace;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Tests.UseCases.Places;

public class PlaceModerationHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly Mock<ICurrentUserService> _mockCurrentUser = new();
    private readonly UserId _currentUserId = new(Guid.NewGuid());

    public PlaceModerationHandlerTests()
    {
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.UserId).Returns(_currentUserId);
    }

    private Mock<IPlaceRepository> PlaceRepository => Mock.Get(_mockUnitOfWork.Object.Places);

    private void GivenAdmin(bool isAdmin) =>
        _mockCurrentUser.Setup(x => x.HasRole(UserRole.Admin)).Returns(isAdmin);

    private void GivenPlace(Place? place) =>
        PlaceRepository.Setup(x => x.GetByIdWithRelationsAsync(It.IsAny<PlaceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(place);

    [Fact]
    public async Task Approve_AsAdmin_ShouldMakePlaceLiveAndSave()
    {
        GivenAdmin(true);
        var place = TestDataBuilder.BuildPlace(new UserId(Guid.NewGuid()));
        GivenPlace(place);

        var response = await new ApprovePlaceHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new ApprovePlaceRequest(place.Id.Value));

        response.Status.Should().Be("Live");
        place.ReviewedBy.Should().Be(_currentUserId);
        Mock.Get(_mockUnitOfWork.Object.ChangeLogs).Verify(x => x.AddAsync(It.IsAny<ListingChangeLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Approve_AsNonAdmin_ShouldThrowForbidden()
    {
        GivenAdmin(false);

        var action = () => new ApprovePlaceHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new ApprovePlaceRequest(Guid.NewGuid()));

        await action.Should().ThrowAsync<ForbiddenException>();
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Approve_PlaceNotFound_ShouldThrowNotFound()
    {
        GivenAdmin(true);
        GivenPlace(null);

        var action = () => new ApprovePlaceHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new ApprovePlaceRequest(Guid.NewGuid()));

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Approve_PlaceAlreadyLive_ShouldThrowInvalidOperation()
    {
        GivenAdmin(true);
        GivenPlace(TestDataBuilder.BuildPlace(new UserId(Guid.NewGuid()), ListingStatus.Live));

        var action = () => new ApprovePlaceHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new ApprovePlaceRequest(Guid.NewGuid()));

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Reject_AsAdmin_ShouldSuspendPlace()
    {
        GivenAdmin(true);
        var place = TestDataBuilder.BuildPlace(new UserId(Guid.NewGuid()));
        GivenPlace(place);

        var response = await new RejectPlaceHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new RejectPlaceRequest(place.Id.Value, "Duplicate listing"));

        response.Status.Should().Be("Suspended");
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reject_AsNonAdmin_ShouldThrowForbidden()
    {
        GivenAdmin(false);

        var action = () => new RejectPlaceHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new RejectPlaceRequest(Guid.NewGuid(), "Spam"));

        await action.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Delete_ByOwner_ShouldArchivePlace()
    {
        GivenAdmin(false);
        var place = TestDataBuilder.BuildPlace(_currentUserId, ListingStatus.Live);
        GivenPlace(place);

        var result = await new DeletePlaceHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new DeletePlaceRequest(place.Id.Value));

        result.Should().BeTrue();
        place.Status.Should().Be(ListingStatus.Archived);
        place.IsDeleted.Should().BeTrue();
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ByAdminWhoIsNotOwner_ShouldArchivePlace()
    {
        GivenAdmin(true);
        var place = TestDataBuilder.BuildPlace(new UserId(Guid.NewGuid()), ListingStatus.Live);
        GivenPlace(place);

        await new DeletePlaceHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new DeletePlaceRequest(place.Id.Value));

        place.Status.Should().Be(ListingStatus.Archived);
    }

    [Fact]
    public async Task Delete_ByStranger_ShouldThrowForbidden()
    {
        GivenAdmin(false);
        GivenPlace(TestDataBuilder.BuildPlace(new UserId(Guid.NewGuid()), ListingStatus.Live));

        var action = () => new DeletePlaceHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new DeletePlaceRequest(Guid.NewGuid()));

        await action.Should().ThrowAsync<ForbiddenException>();
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_NotAuthenticated_ShouldThrowUnauthorized()
    {
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var action = () => new DeletePlaceHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new DeletePlaceRequest(Guid.NewGuid()));

        await action.Should().ThrowAsync<UnauthorizedException>();
    }
}
