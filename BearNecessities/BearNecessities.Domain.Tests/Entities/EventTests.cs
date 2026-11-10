using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Tests.Entities;

public class EventTests
{
    private static Event NewEvent() =>
        Event.Create("  Full Moon Gathering ", EventType.OneTime, "Asia/Bangkok", new UserId(Guid.NewGuid()));

    [Fact]
    public void Create_ShouldTrimTitleAndDefaultToFreeDraft()
    {
        var evt = NewEvent();

        evt.Title.Should().Be("Full Moon Gathering");
        evt.Status.Should().Be(ListingStatus.Draft);
        evt.IsFree.Should().BeTrue();
    }

    [Fact]
    public void Create_BlankTitle_ShouldThrow()
    {
        var action = () => Event.Create(" ", EventType.OneTime, "UTC", new UserId(Guid.NewGuid()));

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SubmitThenApprove_ShouldGoLive()
    {
        var evt = NewEvent();

        evt.Submit();
        evt.Approve(new UserId(Guid.NewGuid()));

        evt.Status.Should().Be(ListingStatus.Live);
    }

    [Fact]
    public void Reject_DraftEvent_ShouldThrow()
    {
        var action = () => NewEvent().Reject(new UserId(Guid.NewGuid()));

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_LiveEvent_ShouldCancelAndBeIdempotent()
    {
        var evt = NewEvent();
        evt.Submit();
        evt.Approve(new UserId(Guid.NewGuid()));

        evt.Cancel();
        evt.Cancel();

        evt.Status.Should().Be(ListingStatus.Cancelled);
    }

    [Fact]
    public void SetPrice_PaidEvent_ShouldNoLongerBeFree()
    {
        var evt = NewEvent();

        evt.SetPrice(25m, "EUR", isFree: false);

        evt.IsFree.Should().BeFalse();
        evt.Price.Should().Be(25m);
        evt.Currency.Should().Be("EUR");
    }

    [Fact]
    public void AddCategory_SixthCategory_ShouldThrow()
    {
        var evt = NewEvent();
        for (var index = 0; index < 5; index++)
            evt.AddCategory(Category.Create($"category-{index}", $"Category {index}"));

        var action = () => evt.AddCategory(Category.Create("one-too-many", "One Too Many"));

        action.Should().Throw<InvalidOperationException>();
    }
}
