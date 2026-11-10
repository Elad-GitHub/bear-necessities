using Moq;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.Tests.Helpers;
using BearNecessities.Application.UseCases.Events.ApproveEvent;
using BearNecessities.Application.UseCases.Events.CancelEvent;
using BearNecessities.Application.UseCases.Events.RejectEvent;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Tests.UseCases.Events;

public class EventModerationHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly Mock<ICurrentUserService> _mockCurrentUser = new();
    private readonly UserId _currentUserId = new(Guid.NewGuid());

    public EventModerationHandlerTests()
    {
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.UserId).Returns(_currentUserId);
    }

    private void GivenAdmin(bool isAdmin) =>
        _mockCurrentUser.Setup(x => x.HasRole(UserRole.Admin)).Returns(isAdmin);

    private Event GivenEvent(ListingStatus status, UserId? ownerId = null)
    {
        var evt = TestDataBuilder.BuildEvent(ownerId ?? new UserId(Guid.NewGuid()), status);
        Mock.Get(_mockUnitOfWork.Object.Events)
            .Setup(x => x.GetByIdAsync(It.IsAny<EventId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        return evt;
    }

    [Fact]
    public async Task Approve_AsAdmin_ShouldMakeEventLive()
    {
        GivenAdmin(true);
        var evt = GivenEvent(ListingStatus.PendingReview);

        var result = await new ApproveEventHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new ApproveEventRequest(evt.Id.Value));

        result.Should().BeTrue();
        evt.Status.Should().Be(ListingStatus.Live);
        evt.ReviewedBy.Should().Be(_currentUserId);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Approve_AsNonAdmin_ShouldThrowUnauthorized()
    {
        GivenAdmin(false);

        var action = () => new ApproveEventHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new ApproveEventRequest(Guid.NewGuid()));

        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Approve_EventNotFound_ShouldThrowNotFound()
    {
        GivenAdmin(true);
        Mock.Get(_mockUnitOfWork.Object.Events)
            .Setup(x => x.GetByIdAsync(It.IsAny<EventId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var action = () => new ApproveEventHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new ApproveEventRequest(Guid.NewGuid()));

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Reject_AsAdmin_ShouldSuspendEvent()
    {
        GivenAdmin(true);
        var evt = GivenEvent(ListingStatus.PendingReview);

        await new RejectEventHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new RejectEventRequest(evt.Id.Value, "Not a real event"));

        evt.Status.Should().Be(ListingStatus.Suspended);
    }

    [Fact]
    public async Task Reject_DraftEvent_ShouldThrowInvalidOperation()
    {
        GivenAdmin(true);
        GivenEvent(ListingStatus.Draft);

        var action = () => new RejectEventHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new RejectEventRequest(Guid.NewGuid(), null));

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Cancel_ByOwner_ShouldCancelEventAndFutureOccurrences()
    {
        GivenAdmin(false);
        var evt = GivenEvent(ListingStatus.Live, _currentUserId);

        var result = await new CancelEventHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new CancelEventRequest(evt.Id.Value));

        result.Should().BeTrue();
        evt.Status.Should().Be(ListingStatus.Cancelled);
        Mock.Get(_mockUnitOfWork.Object.Occurrences).Verify(
            x => x.CancelFutureOccurrencesAsync(evt.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_ByAdminWhoIsNotOwner_ShouldCancelEvent()
    {
        GivenAdmin(true);
        var evt = GivenEvent(ListingStatus.Live);

        await new CancelEventHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new CancelEventRequest(evt.Id.Value));

        evt.Status.Should().Be(ListingStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_ByStranger_ShouldThrowUnauthorized()
    {
        GivenAdmin(false);
        GivenEvent(ListingStatus.Live);

        var action = () => new CancelEventHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new CancelEventRequest(Guid.NewGuid()));

        await action.Should().ThrowAsync<UnauthorizedException>();
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
