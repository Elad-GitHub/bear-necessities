using Moq;
using BearNecessities.Application.Imports;
using BearNecessities.Application.UseCases.Places.GetPlaceDensity;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Tests.UseCases.Places;

public class GetPlaceDensityHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly GetPlaceDensityHandler _handler;

    public GetPlaceDensityHandlerTests()
    {
        _handler = new GetPlaceDensityHandler(_mockUnitOfWork.Object);
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(2, 15)]
    [InlineData(2.9, 15)]
    [InlineData(7, 0.46875)]
    public void CellSizeForZoom_ShouldHalveWithEachZoomLevel(double zoom, double expectedDegrees)
    {
        GetPlaceDensityHandler.CellSizeForZoom(zoom).Should().Be((decimal)expectedDegrees);
    }

    [Fact]
    public void CellSizeForZoom_OutOfRangeZoom_ShouldBeClamped()
    {
        GetPlaceDensityHandler.CellSizeForZoom(-3).Should().Be(GetPlaceDensityHandler.CellSizeForZoom(0));
        GetPlaceDensityHandler.CellSizeForZoom(30).Should().Be(GetPlaceDensityHandler.CellSizeForZoom(14));
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCellsAndTheirTotal()
    {
        var categoryId = Guid.NewGuid();
        Mock.Get(_mockUnitOfWork.Object.Places)
            .Setup(x => x.GetDensityAsync(It.IsAny<GeoViewport>(), It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(categoryId)), 15m, It.IsAny<DateRange?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlaceDensityCell>
            {
                new(38.7m, -9.1m, 1300, 36.9m, -9.5m, 42.1m, -6.2m),
                new(32.6m, -16.9m, 106, 32.6m, -17.2m, 32.9m, -16.7m)
            });

        var response = await _handler.HandleAsync(new GetPlaceDensityRequest(85, 180, -85, -180, 2, [categoryId]));

        response.Cells.Should().HaveCount(2);
        response.TotalCount.Should().Be(1406);
        response.Cells[0].Bounds.Should().Equal(-9.5m, 36.9m, -6.2m, 42.1m);
    }

    [Fact]
    public async Task HandleAsync_InvalidViewport_ShouldThrow()
    {
        var action = () => _handler.HandleAsync(new GetPlaceDensityRequest(95, 0, 0, 0, 3));

        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}

public class ImportReportTests
{
    [Fact]
    public void Combine_ShouldSumCountsAndKeepAllErrors()
    {
        var combined = ImportReport.Combine(
        [
            new ImportReport(100, 2, 0, []),
            new ImportReport(29, 0, 1, ["Epirus: one bad row"]),
            new ImportReport(0, 0, 0, ["Attica: Overpass unavailable after retries."])
        ]);

        combined.Inserted.Should().Be(129);
        combined.SkippedDuplicates.Should().Be(2);
        combined.Failed.Should().Be(1);
        combined.Errors.Should().HaveCount(2);
    }
}
