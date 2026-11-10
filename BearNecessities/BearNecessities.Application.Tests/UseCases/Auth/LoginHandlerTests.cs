using Moq;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.UseCases.Auth.Login;
using BearNecessities.Application.Tests.Helpers;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Tests.UseCases.Auth;

public class LoginHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly LoginHandler _handler;
    private readonly User _testUser;

    public LoginHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockJwtService = new Mock<IJwtService>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();

        _testUser = TestDataBuilder.BuildUser("test@example.com", "Test User");

        _mockUnitOfWork.Setup(x => x.Users.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        _mockJwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns("fake_access_token");
        _mockJwtService.Setup(x => x.CreateRefreshToken(It.IsAny<UserId>()))
            .Returns((UserId userId) => RefreshToken.Create(userId, "fake_refresh_token", DateTime.UtcNow.AddDays(7)));

        _handler = new LoginHandler(_mockUnitOfWork.Object, _mockJwtService.Object, _mockPasswordHasher.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "Password123!");

        // Act
        var response = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        response.AccessToken.Should().Be("fake_access_token");
        response.RefreshToken.Should().Be("fake_refresh_token");
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _mockUnitOfWork.Setup(x => x.Users.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var request = new LoginRequest("notfound@example.com", "Password123!");

        // Act & Assert
        var action = () => _handler.HandleAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        var request = new LoginRequest("test@example.com", "WrongPassword!");

        // Act & Assert
        var action = () => _handler.HandleAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_AndWrongPassword_ShouldReturnSameErrorMessage()
    {
        // Arrange
        var notFoundRequest = new LoginRequest("notfound@example.com", "Password123!");
        var wrongPasswordRequest = new LoginRequest("test@example.com", "WrongPassword!");

        _mockUnitOfWork.Setup(x => x.Users.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Act & Assert
        var notFoundAction = () => _handler.HandleAsync(notFoundRequest, CancellationToken.None);
        var wrongPasswordAction = () => _handler.HandleAsync(wrongPasswordRequest, CancellationToken.None);

        var notFoundEx = await notFoundAction.Should().ThrowAsync<UnauthorizedException>();
        var wrongPasswordEx = await wrongPasswordAction.Should().ThrowAsync<UnauthorizedException>();

        notFoundEx.And.Message.Should().Be(wrongPasswordEx.And.Message);
    }

    [Fact]
    public async Task HandleAsync_DeactivatedUser_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var deactivatedUser = TestDataBuilder.BuildUser("test@example.com", "Test User");
        deactivatedUser.Deactivate();
        _mockUnitOfWork.Setup(x => x.Users.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deactivatedUser);
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        var request = new LoginRequest("test@example.com", "Password123!");

        // Act & Assert
        var action = () => _handler.HandleAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<UnauthorizedException>();
    }
}
