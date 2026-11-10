using Moq;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.Tests.Helpers;
using BearNecessities.Application.UseCases.Reviews.AdminRemoveReview;
using BearNecessities.Application.UseCases.Reviews.CreateReview;
using BearNecessities.Application.UseCases.Reviews.SubmitCounterReview;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Tests.UseCases.Reviews;

public class ReviewHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly Mock<ICurrentUserService> _mockCurrentUser = new();
    private readonly UserId _currentUserId = new(Guid.NewGuid());

    public ReviewHandlerTests()
    {
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.UserId).Returns(_currentUserId);
    }

    private Mock<IReviewRepository> ReviewRepository => Mock.Get(_mockUnitOfWork.Object.Reviews);

    private CreateReviewHandler CreateHandler() => new(_mockUnitOfWork.Object, _mockCurrentUser.Object);

    private void GivenPlaceExists(bool exists) =>
        Mock.Get(_mockUnitOfWork.Object.Places)
            .Setup(x => x.GetByIdAsync(It.IsAny<PlaceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists ? TestDataBuilder.BuildPlace(new UserId(Guid.NewGuid()), ListingStatus.Live) : null);

    private void GivenAlreadyReviewed(bool alreadyReviewed) =>
        ReviewRepository.Setup(x => x.ExistsAsync(It.IsAny<UserId>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(alreadyReviewed);

    [Fact]
    public async Task Create_PlaceReview_ShouldBeRevealedImmediately()
    {
        GivenPlaceExists(true);
        GivenAlreadyReviewed(false);

        var response = await CreateHandler().HandleAsync(new CreateReviewRequest("Place", Guid.NewGuid(), 5, "Lovely", null));

        response.Status.Should().Be("Revealed");
        ReviewRepository.Verify(x => x.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ReviewWithCounterparty_ShouldStayHiddenUntilCounterReview()
    {
        GivenPlaceExists(true);
        GivenAlreadyReviewed(false);

        var response = await CreateHandler().HandleAsync(new CreateReviewRequest("Place", Guid.NewGuid(), 4, null, Guid.NewGuid()));

        response.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Create_SecondReviewOfSameTarget_ShouldThrowValidation()
    {
        GivenPlaceExists(true);
        GivenAlreadyReviewed(true);

        var action = () => CreateHandler().HandleAsync(new CreateReviewRequest("Place", Guid.NewGuid(), 5, null, null));

        await action.Should().ThrowAsync<ValidationException>();
        ReviewRepository.Verify(x => x.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_MissingPlace_ShouldThrowNotFound()
    {
        GivenPlaceExists(false);

        var action = () => CreateHandler().HandleAsync(new CreateReviewRequest("Place", Guid.NewGuid(), 5, null, null));

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData("User", 5)]
    [InlineData("Place", 0)]
    [InlineData("Place", 6)]
    public async Task Create_InvalidTargetTypeOrRating_ShouldThrowValidation(string targetType, int rating)
    {
        var action = () => CreateHandler().HandleAsync(new CreateReviewRequest(targetType, Guid.NewGuid(), rating, null, null));

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Create_NotAuthenticated_ShouldThrowUnauthorized()
    {
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var action = () => CreateHandler().HandleAsync(new CreateReviewRequest("Place", Guid.NewGuid(), 5, null, null));

        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task CounterReview_ByCounterparty_ShouldRevealBothReviews()
    {
        var authorId = new UserId(Guid.NewGuid());
        var original = Review.Create(authorId, "Place", Guid.NewGuid(), 4, null, _currentUserId);
        original.SetAuthorSubmittedAt();
        ReviewRepository.Setup(x => x.GetByIdAsync(original.Id, It.IsAny<CancellationToken>())).ReturnsAsync(original);

        var response = await new SubmitCounterReviewHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new SubmitCounterReviewRequest(original.Id, 5, "Great host"));

        response.Status.Should().Be("Revealed");
        original.RevealedAt.Should().NotBeNull();
        ReviewRepository.Verify(x => x.AddAsync(
            It.Is<Review>(counter => counter.AuthorId == _currentUserId && counter.Status == ReviewStatus.Revealed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CounterReview_BySomeoneElse_ShouldThrowForbidden()
    {
        var original = Review.Create(new UserId(Guid.NewGuid()), "Place", Guid.NewGuid(), 4, null, new UserId(Guid.NewGuid()));
        original.SetAuthorSubmittedAt();
        ReviewRepository.Setup(x => x.GetByIdAsync(original.Id, It.IsAny<CancellationToken>())).ReturnsAsync(original);

        var action = () => new SubmitCounterReviewHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new SubmitCounterReviewRequest(original.Id, 5, null));

        await action.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CounterReview_OnAlreadyRevealedReview_ShouldThrowValidation()
    {
        var original = Review.Create(new UserId(Guid.NewGuid()), "Place", Guid.NewGuid(), 4, null, _currentUserId);
        original.SetAuthorSubmittedAt();
        original.SetCounterSubmittedAt();
        original.TryReveal();
        ReviewRepository.Setup(x => x.GetByIdAsync(original.Id, It.IsAny<CancellationToken>())).ReturnsAsync(original);

        var action = () => new SubmitCounterReviewHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new SubmitCounterReviewRequest(original.Id, 5, null));

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AdminRemove_AsAdmin_ShouldMarkReviewRemoved()
    {
        _mockCurrentUser.Setup(x => x.HasRole(UserRole.Admin)).Returns(true);
        var review = TestDataBuilder.BuildReview(new UserId(Guid.NewGuid()), "Place", Guid.NewGuid());
        ReviewRepository.Setup(x => x.GetByIdAsync(review.Id, It.IsAny<CancellationToken>())).ReturnsAsync(review);

        var result = await new AdminRemoveReviewHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new AdminRemoveReviewRequest(review.Id, "Abusive"));

        result.Should().BeTrue();
        review.Status.Should().Be(ReviewStatus.Removed);
    }

    [Fact]
    public async Task AdminRemove_AsNonAdmin_ShouldThrowForbidden()
    {
        _mockCurrentUser.Setup(x => x.HasRole(UserRole.Admin)).Returns(false);

        var action = () => new AdminRemoveReviewHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new AdminRemoveReviewRequest(Guid.NewGuid(), "Abusive"));

        await action.Should().ThrowAsync<ForbiddenException>();
    }
}
