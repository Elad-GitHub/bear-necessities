using FluentAssertions;
using Moq;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.UseCases.Auth.Register;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.Tests.UseCases.Auth;

public class RegisterHandlerTests
{
    private const string ValidSocialUrl = "https://instagram.com/test.user";

    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly RegisterHandler _handler;

    public RegisterHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();

        _mockUnitOfWork.Setup(x => x.Users.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUnitOfWork.Setup(x => x.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockPasswordHasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        _handler = new RegisterHandler(_mockUnitOfWork.Object, _mockPasswordHasher.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ShouldCreateUser()
    {
        var request = new RegisterRequest("test@example.com", "Test User", "Password123!", ValidSocialUrl);

        await _handler.HandleAsync(request, CancellationToken.None);

        _mockUnitOfWork.Verify(x => x.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ShouldReturnPendingMembership()
    {
        var request = new RegisterRequest("test@example.com", "Test User", "Password123!", ValidSocialUrl);

        var response = await _handler.HandleAsync(request, CancellationToken.None);

        response.MembershipStatus.Should().Be("Pending");
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_ShouldThrowValidationException()
    {
        _mockUnitOfWork.Setup(x => x.Users.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var request = new RegisterRequest("test@example.com", "Test User", "Password123!", ValidSocialUrl);

        var action = () => _handler.HandleAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_InvalidEmail_ShouldThrowValidationException()
    {
        var request = new RegisterRequest("invalid-email", "Test User", "Password123!", ValidSocialUrl);

        var action = () => _handler.HandleAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_PasswordTooShort_ShouldThrowValidationException()
    {
        var request = new RegisterRequest("test@example.com", "Test User", "Pass1!", ValidSocialUrl);

        var action = () => _handler.HandleAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_EmptyDisplayName_ShouldThrowValidationException()
    {
        var request = new RegisterRequest("test@example.com", "", "Password123!", ValidSocialUrl);

        var action = () => _handler.HandleAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_MissingSocialProfile_ShouldThrowValidationException()
    {
        var request = new RegisterRequest("test@example.com", "Test User", "Password123!", "");

        var action = () => _handler.HandleAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_NonSocialUrl_ShouldThrowValidationException()
    {
        var request = new RegisterRequest("test@example.com", "Test User", "Password123!", "https://example.com/me");

        var action = () => _handler.HandleAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
    }
}
