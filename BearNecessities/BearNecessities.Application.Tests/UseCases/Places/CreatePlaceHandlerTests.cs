using Moq;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.UseCases.Places.CreatePlace;
using BearNecessities.Application.Tests.Helpers;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Tests.UseCases.Places;

public class CreatePlaceHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly CreatePlaceHandler _handler;
    private readonly UserId _testUserId;

    public CreatePlaceHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _testUserId = new UserId(Guid.NewGuid());

        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.UserId).Returns(_testUserId);

        var mockCategoryRepo = new Mock<ICategoryRepository>();
        mockCategoryRepo.Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Category.Create("test", "Test"));

        _mockUnitOfWork.Setup(x => x.Places.AddAsync(It.IsAny<Place>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.Categories).Returns(mockCategoryRepo.Object);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new CreatePlaceHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ShouldCreatePlace()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var request = new CreatePlaceRequest("Test Place", null, 48.8566m, 2.3522m, null, "Paris", "France", "FR", "UTC", false, new List<Guid> { categoryId }, null);

        // Act
        var response = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(x => x.Places.AddAsync(It.IsAny<Place>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ShouldReturnPendingReviewStatus()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var request = new CreatePlaceRequest("Test Place", null, 48.8566m, 2.3522m, null, "Paris", "France", "FR", "UTC", false, new List<Guid> { categoryId }, null);

        // Act
        var response = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        response.Status.Should().Be("PendingReview");
    }

    [Fact]
    public async Task HandleAsync_NotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);
        var request = new CreatePlaceRequest("Test Place", null, 48.8566m, 2.3522m, null, "Paris", "France", "FR", "UTC", false, new List<Guid> { Guid.NewGuid() }, null);

        // Act & Assert
        var action = () => _handler.HandleAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task HandleAsync_InvalidLatitude_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreatePlaceRequest("Test Place", null, 91m, 2.3522m, null, "Paris", "France", "FR", "UTC", false, new List<Guid> { Guid.NewGuid() }, null);

        // Act & Assert
        var action = () => _handler.HandleAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_TooManyCategories_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreatePlaceRequest("Test Place", null, 48.8566m, 2.3522m, null, "Paris", "France", "FR", "UTC", false,
            Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToList(), null);

        // Act & Assert
        var action = () => _handler.HandleAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_NoCategoriesProvided_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreatePlaceRequest("Test Place", null, 48.8566m, 2.3522m, null, "Paris", "France", "FR", "UTC", false, new List<Guid>(), null);

        // Act & Assert
        var action = () => _handler.HandleAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_CategoryNotFound_ShouldThrowValidationException()
    {
        // Arrange
        var mockCategoryRepo = new Mock<ICategoryRepository>();
        mockCategoryRepo.Setup(x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);
        _mockUnitOfWork.Setup(x => x.Categories).Returns(mockCategoryRepo.Object);

        var request = new CreatePlaceRequest("Test Place", null, 48.8566m, 2.3522m, null, "Paris", "France", "FR", "UTC", false, new List<Guid> { Guid.NewGuid() }, null);

        // Act & Assert
        var action = () => _handler.HandleAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
    }
}
