using Moq;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.Tests.Helpers;
using BearNecessities.Application.UseCases.Users.MyProfile;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Tests.UseCases.Users;

public class UpdateMyProfileHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly Mock<ICurrentUserService> _mockCurrentUser = new();
    private readonly User _user = TestDataBuilder.BuildUser("bear@test.com", "Old Name");
    private readonly UpdateMyProfileHandler _handler;

    public UpdateMyProfileHandlerTests()
    {
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.UserId).Returns(_user.Id);
        Mock.Get(_mockUnitOfWork.Object.Users)
            .Setup(x => x.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_user);
        _handler = new UpdateMyProfileHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object);
    }

    private Media GivenUploadedPhoto(Guid ownerId, MediaOwnerType ownerType = MediaOwnerType.User)
    {
        var media = Media.Create(ownerType, ownerId, "user/photo.jpg", "https://cdn/photo.jpg", "image/jpeg", 1000, _user.Id);
        Mock.Get(_mockUnitOfWork.Object.Media)
            .Setup(x => x.GetByIdAsync(media.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(media);
        return media;
    }

    [Fact]
    public async Task HandleAsync_NameAndBio_ShouldUpdateAndSave()
    {
        var profile = await _handler.HandleAsync(new UpdateMyProfileRequest("  Honey Bear ", "Loves ecstatic dance", null));

        profile.DisplayName.Should().Be("Honey Bear");
        profile.Bio.Should().Be("Loves ecstatic dance");
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_OwnPhoto_ShouldBecomeTheAvatar()
    {
        var photo = GivenUploadedPhoto(_user.Id.Value);

        var profile = await _handler.HandleAsync(new UpdateMyProfileRequest("Honey Bear", null, photo.Id));

        profile.AvatarUrl.Should().Be($"/api/v1/media/{photo.Id}/content");
    }

    [Theory]
    [InlineData(MediaOwnerType.User, false)]
    [InlineData(MediaOwnerType.Place, true)]
    public async Task HandleAsync_PhotoThatIsNotOfThisMember_ShouldBeForbidden(MediaOwnerType ownerType, bool isOwnId)
    {
        var photo = GivenUploadedPhoto(isOwnId ? _user.Id.Value : Guid.NewGuid(), ownerType);

        var action = () => _handler.HandleAsync(new UpdateMyProfileRequest("Honey Bear", null, photo.Id));

        await action.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task HandleAsync_RemoveAvatar_ShouldClearIt()
    {
        _user.SetAvatarUrl("/api/v1/media/old/content");

        var profile = await _handler.HandleAsync(new UpdateMyProfileRequest("Honey Bear", null, null, RemoveAvatar: true));

        profile.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_BlankName_ShouldThrowValidation()
    {
        var action = () => _handler.HandleAsync(new UpdateMyProfileRequest("   ", null, null));

        await action.Should().ThrowAsync<ValidationException>();
    }
}
