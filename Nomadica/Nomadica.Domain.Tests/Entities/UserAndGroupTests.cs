using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Create_ShouldBeActivePendingTraveler()
    {
        var user = User.Create("river@test.com", "River");

        user.IsActive.Should().BeTrue();
        user.MembershipStatus.Should().Be(MembershipStatus.Pending);
        user.Roles.Should().ContainSingle().Which.Should().Be(UserRole.Traveler);
    }

    [Theory]
    [InlineData("", "River")]
    [InlineData("river@test.com", " ")]
    public void Create_BlankEmailOrName_ShouldThrow(string email, string displayName)
    {
        var action = () => User.Create(email, displayName);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddRole_Twice_ShouldNotDuplicate()
    {
        var user = User.Create("river@test.com", "River");

        user.AddRole(UserRole.Organizer);
        user.AddRole(UserRole.Organizer);

        user.Roles.Should().BeEquivalentTo(new[] { UserRole.Traveler, UserRole.Organizer });
    }

    [Fact]
    public void ApproveAndRejectMembership_ShouldUpdateStatus()
    {
        var approved = User.Create("a@test.com", "A");
        var rejected = User.Create("b@test.com", "B");

        approved.ApproveMembership();
        rejected.RejectMembership();

        approved.MembershipStatus.Should().Be(MembershipStatus.Approved);
        rejected.MembershipStatus.Should().Be(MembershipStatus.Rejected);
    }

    [Fact]
    public void Deactivate_ShouldMarkInactive()
    {
        var user = User.Create("river@test.com", "River");

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }
}

public class CommunityGroupTests
{
    private static CommunityGroup NewGroup() =>
        CommunityGroup.Create("Sound Healers Goa", 15.3m, 74.0m, new UserId(Guid.NewGuid()));

    [Fact]
    public void Create_ShouldStartWithNoMembers()
    {
        NewGroup().MemberCount.Should().Be(0);
    }

    [Theory]
    [InlineData(" ", 0, 0)]
    public void Create_BlankName_ShouldThrow(string name, int lat, int lng)
    {
        var action = () => CommunityGroup.Create(name, lat, lng, new UserId(Guid.NewGuid()));

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_OutOfRangeLatitude_ShouldThrow()
    {
        var action = () => CommunityGroup.Create("Group", 91m, 0m, new UserId(Guid.NewGuid()));

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DecrementMemberCount_AtZero_ShouldStayAtZero()
    {
        var group = NewGroup();

        group.DecrementMemberCount();

        group.MemberCount.Should().Be(0);
    }

    [Fact]
    public void IncrementThenDecrement_ShouldTrackMembers()
    {
        var group = NewGroup();

        group.IncrementMemberCount();
        group.IncrementMemberCount();
        group.DecrementMemberCount();

        group.MemberCount.Should().Be(1);
    }

    [Fact]
    public void GroupMembership_Create_ShouldDefaultToMemberRole()
    {
        var group = NewGroup();

        var membership = GroupMembership.Create(group.Id, new UserId(Guid.NewGuid()));

        membership.Role.Should().Be(GroupRole.Member);
        membership.GroupId.Should().Be(group.Id);
    }

    [Fact]
    public void GroupPost_TooLongBody_ShouldThrow()
    {
        var action = () => GroupPost.Create(NewGroup().Id, new UserId(Guid.NewGuid()), new string('a', GroupPost.MaxBodyLength + 1));

        action.Should().Throw<ArgumentException>();
    }
}
