using Moq;
using Nomadica.Application.Tests.Helpers;
using Nomadica.Application.UseCases.Events.SearchEvents;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.Tests.UseCases.Events;

public class SearchEventsHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly SearchEventsHandler _handler;

    public SearchEventsHandlerTests()
    {
        _handler = new SearchEventsHandler(_mockUnitOfWork.Object);
    }

    private Mock<IEventRepository> EventRepository => Mock.Get(_mockUnitOfWork.Object.Events);

    [Fact]
    public async Task HandleAsync_ValidQuery_ShouldReturnMatchingEvents()
    {
        var evt = TestDataBuilder.BuildEvent(new UserId(Guid.NewGuid()), ListingStatus.Live);
        EventRepository.Setup(x => x.SearchUpcomingByTextAsync("moon", 6, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Event> { evt });

        var response = await _handler.HandleAsync(new SearchEventsRequest("  moon "));

        response.Items.Should().ContainSingle().Which.Id.Should().Be(evt.Id.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("a")]
    public async Task HandleAsync_TooShortQuery_ShouldNotHitRepository(string? query)
    {
        var response = await _handler.HandleAsync(new SearchEventsRequest(query));

        response.Items.Should().BeEmpty();
        EventRepository.Verify(x => x.SearchUpcomingByTextAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_HugeLimit_ShouldBeCappedAt20()
    {
        EventRepository.Setup(x => x.SearchUpcomingByTextAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Event>());

        await _handler.HandleAsync(new SearchEventsRequest("dance", 500));

        EventRepository.Verify(x => x.SearchUpcomingByTextAsync("dance", 20, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
