using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Tests.Entities;

public class PlaceTests
{
    private static Place NewPlace() =>
        Place.Create("  Sunrise Yoga Shala  ", 37.0m, 25.0m, "Europe/Athens", new UserId(Guid.NewGuid()));

    [Fact]
    public void Create_ShouldTrimNameAndStartAsDraftUserListing()
    {
        var place = NewPlace();

        place.Name.Should().Be("Sunrise Yoga Shala");
        place.Status.Should().Be(ListingStatus.Draft);
        place.Source.Should().Be(ListingSourceType.User);
        place.IsVerified.Should().BeFalse();
    }

    [Theory]
    [InlineData("", 0, 0, "UTC")]
    [InlineData("Name", 0, 0, " ")]
    public void Create_BlankNameOrTimezone_ShouldThrow(string name, int lat, int lng, string timezone)
    {
        var action = () => Place.Create(name, lat, lng, timezone, new UserId(Guid.NewGuid()));

        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(90.1, 0)]
    [InlineData(-90.1, 0)]
    [InlineData(0, 180.1)]
    [InlineData(0, -180.1)]
    public void Create_OutOfRangeCoordinates_ShouldThrow(double lat, double lng)
    {
        var action = () => Place.Create("Name", (decimal)lat, (decimal)lng, "UTC", new UserId(Guid.NewGuid()));

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SubmitThenApprove_ShouldGoLiveAndVerify()
    {
        var place = NewPlace();
        var reviewer = new UserId(Guid.NewGuid());

        place.Submit();
        place.Approve(reviewer);

        place.Status.Should().Be(ListingStatus.Live);
        place.IsVerified.Should().BeTrue();
        place.ReviewedBy.Should().Be(reviewer);
        place.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void Approve_DraftPlace_ShouldThrow()
    {
        var action = () => NewPlace().Approve(new UserId(Guid.NewGuid()));

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Submit_PlaceAlreadyPending_ShouldThrow()
    {
        var place = NewPlace();
        place.Submit();

        var action = () => place.Submit();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reject_PendingPlace_ShouldSuspend()
    {
        var place = NewPlace();
        place.Submit();

        place.Reject(new UserId(Guid.NewGuid()));

        place.Status.Should().Be(ListingStatus.Suspended);
        place.IsVerified.Should().BeFalse();
    }

    [Fact]
    public void RequestReview_LivePlace_ShouldClearReviewAndVerification()
    {
        var place = NewPlace();
        place.Submit();
        place.Approve(new UserId(Guid.NewGuid()));

        place.RequestReview();

        place.Status.Should().Be(ListingStatus.PendingReview);
        place.ReviewedBy.Should().BeNull();
        place.ReviewedAt.Should().BeNull();
        place.IsVerified.Should().BeFalse();
    }

    [Fact]
    public void AddCategory_SameCategoryTwice_ShouldKeepOne()
    {
        var place = NewPlace();
        var category = Category.Create("yoga", "Yoga");

        place.AddCategory(category);
        place.AddCategory(category);

        place.Categories.Should().ContainSingle();
    }

    [Fact]
    public void AddCategory_SixthCategory_ShouldThrow()
    {
        var place = NewPlace();
        for (var index = 0; index < 5; index++)
            place.AddCategory(Category.Create($"category-{index}", $"Category {index}"));

        var action = () => place.AddCategory(Category.Create("one-too-many", "One Too Many"));

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Archive_ShouldSoftDeleteAndBeIdempotent()
    {
        var place = NewPlace();

        place.Archive();
        var firstDeletedAt = place.DeletedAt;
        place.Archive();

        place.Status.Should().Be(ListingStatus.Archived);
        place.IsDeleted.Should().BeTrue();
        place.DeletedAt.Should().Be(firstDeletedAt);
    }
}
