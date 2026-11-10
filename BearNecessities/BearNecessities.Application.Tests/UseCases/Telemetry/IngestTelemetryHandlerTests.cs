using Moq;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.Telemetry;
using BearNecessities.Application.UseCases.Telemetry.IngestTelemetry;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Tests.UseCases.Telemetry;

public class IngestTelemetryHandlerTests
{
    private readonly Mock<ITelemetrySink> _mockSink = new();
    private readonly Mock<ICurrentUserService> _mockCurrentUser = new();
    private readonly IngestTelemetryHandler _handler;
    private IReadOnlyList<TelemetryEvent>? _captured;

    public IngestTelemetryHandlerTests()
    {
        _mockSink
            .Setup(sink => sink.TrackBatchAsync(It.IsAny<IReadOnlyList<TelemetryEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<TelemetryEvent>, CancellationToken>((events, _) => _captured = events)
            .Returns(Task.CompletedTask);

        _handler = new IngestTelemetryHandler(_mockSink.Object, _mockCurrentUser.Object);
    }

    private static ClientTelemetryEvent Event(string name = "click") =>
        new("event", name, "/map", "sess-1", null);

    [Fact]
    public async Task HandleAsync_EmptyBatch_ReturnsZeroAndSkipsSink()
    {
        var response = await _handler.HandleAsync(new IngestTelemetryRequest(new List<ClientTelemetryEvent>()), CancellationToken.None);

        response.Accepted.Should().Be(0);
        _mockSink.Verify(sink => sink.TrackBatchAsync(It.IsAny<IReadOnlyList<TelemetryEvent>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NullEvents_ReturnsZeroAndSkipsSink()
    {
        var response = await _handler.HandleAsync(new IngestTelemetryRequest(null), CancellationToken.None);

        response.Accepted.Should().Be(0);
        _mockSink.Verify(sink => sink.TrackBatchAsync(It.IsAny<IReadOnlyList<TelemetryEvent>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_StampsUserIdFromCurrentUser_NotFromClient()
    {
        var authenticatedUserId = Guid.NewGuid();
        _mockCurrentUser.Setup(user => user.UserId).Returns(new UserId(authenticatedUserId));

        await _handler.HandleAsync(new IngestTelemetryRequest(new[] { Event() }), CancellationToken.None);

        _captured.Should().NotBeNull();
        _captured![0].UserId.Should().Be(authenticatedUserId);
    }

    [Fact]
    public async Task HandleAsync_AnonymousUser_LeavesUserIdNull()
    {
        _mockCurrentUser.Setup(user => user.UserId).Returns((UserId?)null);

        await _handler.HandleAsync(new IngestTelemetryRequest(new[] { Event() }), CancellationToken.None);

        _captured![0].UserId.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_CapsBatchAtFifty()
    {
        var overLimit = Enumerable.Range(0, 120).Select(_ => Event()).ToList();

        var response = await _handler.HandleAsync(new IngestTelemetryRequest(overLimit), CancellationToken.None);

        response.Accepted.Should().Be(50);
        _captured!.Count.Should().Be(50);
    }

    [Fact]
    public async Task HandleAsync_BlankType_DefaultsToEvent()
    {
        var withBlankType = new ClientTelemetryEvent("  ", "something", null, null, null);

        await _handler.HandleAsync(new IngestTelemetryRequest(new[] { withBlankType }), CancellationToken.None);

        _captured![0].Type.Should().Be("event");
    }
}
