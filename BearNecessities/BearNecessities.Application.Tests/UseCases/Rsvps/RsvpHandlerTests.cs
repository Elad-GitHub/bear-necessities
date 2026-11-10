using Moq;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.Tests.Helpers;
using BearNecessities.Application.UseCases.Rsvps.CancelRsvp;
using BearNecessities.Application.UseCases.Rsvps.CreateOrUpdateRsvp;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Tests.UseCases.Rsvps;

public class RsvpHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly Mock<ICurrentUserService> _mockCurrentUser = new();
    private readonly UserId _currentUserId = new(Guid.NewGuid());

    public RsvpHandlerTests()
    {
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.UserId).Returns(_currentUserId);
    }

    private Mock<IRsvpRepository> RsvpRepository => Mock.Get(_mockUnitOfWork.Object.Rsvps);

    private CreateOrUpdateRsvpHandler CreateHandler() => new(_mockUnitOfWork.Object, _mockCurrentUser.Object);

    private Event GivenEvent(int? maxCapacity = null, int alreadyGoing = 0, Rsvp? existingRsvp = null)
    {
        var evt = TestDataBuilder.BuildEvent(new UserId(Guid.NewGuid()), ListingStatus.Live);
        evt.SetMaxCapacity(maxCapacity);
        Mock.Get(_mockUnitOfWork.Object.Events)
            .Setup(x => x.GetByIdAsync(It.IsAny<EventId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        RsvpRepository.Setup(x => x.GetCountByEventAsync(It.IsAny<EventId>(), RsvpStatus.Going, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alreadyGoing);
        RsvpRepository.Setup(x => x.GetUserRsvpForEventAsync(It.IsAny<UserId>(), It.IsAny<EventId>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRsvp);
        return evt;
    }

    [Fact]
    public async Task CreateOrUpdate_FirstRsvp_ShouldCreate()
    {
        var evt = GivenEvent();

        var response = await CreateHandler().HandleAsync(new CreateOrUpdateRsvpRequest(evt.Id.Value, RsvpStatus.Going, 2));

        response.Created.Should().BeTrue();
        response.PartySize.Should().Be(2);
        RsvpRepository.Verify(x => x.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdate_ExistingRsvp_ShouldUpdateInPlace()
    {
        var existing = Rsvp.Create(_currentUserId, new EventId(Guid.NewGuid()), RsvpStatus.Maybe);
        var evt = GivenEvent(existingRsvp: existing);

        var response = await CreateHandler().HandleAsync(new CreateOrUpdateRsvpRequest(evt.Id.Value, RsvpStatus.Going));

        response.Created.Should().BeFalse();
        response.RsvpId.Should().Be(existing.Id);
        existing.Status.Should().Be(RsvpStatus.Going);
        RsvpRepository.Verify(x => x.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrUpdate_EventFull_ShouldThrowValidation()
    {
        var evt = GivenEvent(maxCapacity: 10, alreadyGoing: 9);

        var action = () => CreateHandler().HandleAsync(new CreateOrUpdateRsvpRequest(evt.Id.Value, RsvpStatus.Going, 2));

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateOrUpdate_GrowingOwnGoingParty_ShouldNotDoubleCountExistingSeats()
    {
        // 10 seats, 9 taken of which 3 are this user's party: growing to 4 needs exactly one more seat
        var existing = Rsvp.Create(_currentUserId, new EventId(Guid.NewGuid()), RsvpStatus.Going, 3);
        var evt = GivenEvent(maxCapacity: 10, alreadyGoing: 9, existingRsvp: existing);

        var response = await CreateHandler().HandleAsync(new CreateOrUpdateRsvpRequest(evt.Id.Value, RsvpStatus.Going, 4));

        response.PartySize.Should().Be(4);
    }

    [Fact]
    public async Task CreateOrUpdate_MaybeOnFullEvent_ShouldBeAllowed()
    {
        var evt = GivenEvent(maxCapacity: 10, alreadyGoing: 10);

        var response = await CreateHandler().HandleAsync(new CreateOrUpdateRsvpRequest(evt.Id.Value, RsvpStatus.Maybe));

        response.Status.Should().Be(RsvpStatus.Maybe);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public async Task CreateOrUpdate_InvalidPartySize_ShouldThrowValidation(int partySize)
    {
        var action = () => CreateHandler().HandleAsync(new CreateOrUpdateRsvpRequest(Guid.NewGuid(), RsvpStatus.Going, partySize));

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateOrUpdate_MissingEvent_ShouldThrowNotFound()
    {
        Mock.Get(_mockUnitOfWork.Object.Events)
            .Setup(x => x.GetByIdAsync(It.IsAny<EventId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var action = () => CreateHandler().HandleAsync(new CreateOrUpdateRsvpRequest(Guid.NewGuid(), RsvpStatus.Going));

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Cancel_OwnRsvp_ShouldDelete()
    {
        var rsvp = Rsvp.Create(_currentUserId, new EventId(Guid.NewGuid()), RsvpStatus.Going);
        RsvpRepository.Setup(x => x.GetByIdAsync(rsvp.Id, It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);

        var result = await new CancelRsvpHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new CancelRsvpRequest(rsvp.Id));

        result.Should().BeTrue();
        RsvpRepository.Verify(x => x.DeleteAsync(rsvp, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_SomeoneElsesRsvp_ShouldThrowForbidden()
    {
        _mockCurrentUser.Setup(x => x.HasRole(UserRole.Admin)).Returns(false);
        var rsvp = Rsvp.Create(new UserId(Guid.NewGuid()), new EventId(Guid.NewGuid()), RsvpStatus.Going);
        RsvpRepository.Setup(x => x.GetByIdAsync(rsvp.Id, It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);

        var action = () => new CancelRsvpHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new CancelRsvpRequest(rsvp.Id));

        await action.Should().ThrowAsync<ForbiddenException>();
        RsvpRepository.Verify(x => x.DeleteAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
