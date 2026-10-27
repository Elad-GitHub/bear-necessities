using Moq;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Application.UseCases.SavedItems.ToggleSave;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.Tests.UseCases.SavedItems;

public class ToggleSaveHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly Mock<ICurrentUserService> _mockCurrentUser = new();
    private readonly UserId _currentUserId = new(Guid.NewGuid());
    private readonly ToggleSaveHandler _handler;

    public ToggleSaveHandlerTests()
    {
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.UserId).Returns(_currentUserId);
        _handler = new ToggleSaveHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object);
    }

    private Mock<ISavedItemRepository> SavedItemRepository => Mock.Get(_mockUnitOfWork.Object.SavedItems);

    private void GivenExisting(SavedItem? existing) =>
        SavedItemRepository.Setup(x => x.GetAsync(It.IsAny<UserId>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

    [Fact]
    public async Task HandleAsync_NotSavedYet_ShouldSave()
    {
        GivenExisting(null);

        var response = await _handler.HandleAsync(new ToggleSaveRequest("Place", Guid.NewGuid(), "WantToGo"));

        response.IsSaved.Should().BeTrue();
        response.SaveType.Should().Be("WantToGo");
        SavedItemRepository.Verify(x => x.SaveAsync(It.IsAny<SavedItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_SameTypeAlreadySaved_ShouldUnsave()
    {
        var existing = SavedItem.Create(_currentUserId, "Place", Guid.NewGuid(), SavedItemType.Visited);
        GivenExisting(existing);

        var response = await _handler.HandleAsync(new ToggleSaveRequest("Place", existing.TargetId, "Visited"));

        response.IsSaved.Should().BeFalse();
        response.SaveType.Should().BeNull();
        SavedItemRepository.Verify(x => x.Remove(existing), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DifferentTypeAlreadySaved_ShouldSwitchType()
    {
        var existing = SavedItem.Create(_currentUserId, "Place", Guid.NewGuid(), SavedItemType.WantToGo);
        GivenExisting(existing);

        var response = await _handler.HandleAsync(new ToggleSaveRequest("Place", existing.TargetId, "visitedloved"));

        response.IsSaved.Should().BeTrue();
        response.SaveType.Should().Be("VisitedLoved");
        existing.ItemType.Should().Be(SavedItemType.VisitedLoved);
        SavedItemRepository.Verify(x => x.Remove(It.IsAny<SavedItem>()), Times.Never);
    }

    [Theory]
    [InlineData("Group", "WantToGo")]
    [InlineData("Place", "Favourite")]
    public async Task HandleAsync_InvalidTargetOrSaveType_ShouldThrowValidation(string targetType, string saveType)
    {
        var action = () => _handler.HandleAsync(new ToggleSaveRequest(targetType, Guid.NewGuid(), saveType));

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_NotAuthenticated_ShouldThrowUnauthorized()
    {
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var action = () => _handler.HandleAsync(new ToggleSaveRequest("Place", Guid.NewGuid()));

        await action.Should().ThrowAsync<UnauthorizedException>();
    }
}
