using Moq;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.Tests.Helpers;
using BearNecessities.Application.UseCases.Media.ConfirmUpload;
using BearNecessities.Application.UseCases.Reviews.CreateReview;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Tests.UseCases.Gallery;

public class GalleryPhotoTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly Mock<ICurrentUserService> _mockCurrentUser = new();
    private readonly Mock<IStorageService> _mockStorage = new();
    private readonly UserId _memberId = new(Guid.NewGuid());

    public GalleryPhotoTests()
    {
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.UserId).Returns(_memberId);
        _mockStorage.Setup(x => x.ObjectExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private Domain.Entities.Media GivenUpload(MediaOwnerType ownerType, Guid ownerId, UserId? uploadedBy = null)
    {
        var media = Domain.Entities.Media.Create(ownerType, ownerId, "place/photo.jpg", "https://cdn/photo.jpg", "image/jpeg", 2048, uploadedBy ?? _memberId);
        Mock.Get(_mockUnitOfWork.Object.Media)
            .Setup(x => x.GetByIdAsync(media.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(media);
        return media;
    }

    private Task<ConfirmUploadResponse> Confirm(Domain.Entities.Media media) =>
        new ConfirmUploadHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object, _mockStorage.Object)
            .HandleAsync(new ConfirmUploadRequest(media.Id));

    [Fact]
    public async Task Confirm_MemberAddsPlacePhoto_ShouldWaitForApproval()
    {
        var media = GivenUpload(MediaOwnerType.Place, Guid.NewGuid());

        var response = await Confirm(media);

        response.ModerationStatus.Should().Be("Pending");
    }

    [Fact]
    public async Task Confirm_AdminAddsPlacePhoto_ShouldGoLive()
    {
        _mockCurrentUser.Setup(x => x.HasRole(UserRole.Admin)).Returns(true);
        var media = GivenUpload(MediaOwnerType.Place, Guid.NewGuid());

        var response = await Confirm(media);

        response.ModerationStatus.Should().Be("Approved");
    }

    [Fact]
    public async Task Confirm_ProfilePhoto_ShouldGoLive()
    {
        var media = GivenUpload(MediaOwnerType.User, _memberId.Value);

        var response = await Confirm(media);

        response.ModerationStatus.Should().Be("Approved");
    }

    private CreateReviewHandler GivenReviewablePlace(Guid placeId)
    {
        Mock.Get(_mockUnitOfWork.Object.Places)
            .Setup(x => x.GetByIdAsync(new PlaceId(placeId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataBuilder.BuildPlace(new UserId(Guid.NewGuid()), ListingStatus.Live));
        return new CreateReviewHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object);
    }

    private void GivenPhotosLookup(params Domain.Entities.Media[] photos) =>
        Mock.Get(_mockUnitOfWork.Object.Media)
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(photos.ToList());

    [Fact]
    public async Task CreateReview_WithOwnPhotoOfThePlace_ShouldAttachIt()
    {
        var placeId = Guid.NewGuid();
        var handler = GivenReviewablePlace(placeId);
        var photo = GivenUpload(MediaOwnerType.Place, placeId);
        GivenPhotosLookup(photo);

        var response = await handler.HandleAsync(new CreateReviewRequest("Place", placeId, 5, "Magic sunsets", null, [photo.Id]));

        photo.ReviewId.Should().Be(response.ReviewId);
    }

    [Fact]
    public async Task CreateReview_WithSomeoneElsesPhoto_ShouldBeForbidden()
    {
        var placeId = Guid.NewGuid();
        var handler = GivenReviewablePlace(placeId);
        var photo = GivenUpload(MediaOwnerType.Place, placeId, uploadedBy: new UserId(Guid.NewGuid()));
        GivenPhotosLookup(photo);

        var action = () => handler.HandleAsync(new CreateReviewRequest("Place", placeId, 5, null, null, [photo.Id]));

        await action.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CreateReview_WithPhotoOfAnotherPlace_ShouldBeForbidden()
    {
        var placeId = Guid.NewGuid();
        var handler = GivenReviewablePlace(placeId);
        var photo = GivenUpload(MediaOwnerType.Place, Guid.NewGuid());
        GivenPhotosLookup(photo);

        var action = () => handler.HandleAsync(new CreateReviewRequest("Place", placeId, 5, null, null, [photo.Id]));

        await action.Should().ThrowAsync<ForbiddenException>();
    }
}
