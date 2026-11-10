using Moq;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.UseCases.Groups.CreateGroupPost;
using BearNecessities.Application.UseCases.Groups.JoinGroup;
using BearNecessities.Application.UseCases.Groups.LeaveGroup;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Tests.UseCases.Groups;

public class GroupHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly Mock<ICurrentUserService> _mockCurrentUser = new();
    private readonly UserId _currentUserId = new(Guid.NewGuid());
    private readonly CommunityGroup _group;

    public GroupHandlerTests()
    {
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.UserId).Returns(_currentUserId);

        _group = CommunityGroup.Create("Ecstatic Dance Lisbon", 38.72m, -9.14m, new UserId(Guid.NewGuid()));
        _group.IncrementMemberCount();
        GroupRepository.Setup(x => x.GetByIdAsync(_group.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_group);
    }

    private Mock<IGroupRepository> GroupRepository => Mock.Get(_mockUnitOfWork.Object.Groups);

    private void GivenMembership(GroupMembership? membership) =>
        GroupRepository.Setup(x => x.GetMembershipAsync(_group.Id, _currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);

    [Fact]
    public async Task Join_NewMember_ShouldAddMembershipAndIncrementCount()
    {
        GivenMembership(null);

        var response = await new JoinGroupHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new JoinGroupRequest(_group.Id.Value));

        response.IsMember.Should().BeTrue();
        response.MemberCount.Should().Be(2);
        GroupRepository.Verify(x => x.AddMembershipAsync(It.IsAny<GroupMembership>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Join_ExistingMember_ShouldBeIdempotent()
    {
        GivenMembership(GroupMembership.Create(_group.Id, _currentUserId));

        var response = await new JoinGroupHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new JoinGroupRequest(_group.Id.Value));

        response.MemberCount.Should().Be(1);
        GroupRepository.Verify(x => x.AddMembershipAsync(It.IsAny<GroupMembership>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Join_MissingGroup_ShouldThrowNotFound()
    {
        var action = () => new JoinGroupHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new JoinGroupRequest(Guid.NewGuid()));

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Leave_Member_ShouldRemoveMembershipAndDecrementCount()
    {
        var membership = GroupMembership.Create(_group.Id, _currentUserId);
        GivenMembership(membership);

        var response = await new LeaveGroupHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new LeaveGroupRequest(_group.Id.Value));

        response.IsMember.Should().BeFalse();
        response.MemberCount.Should().Be(0);
        GroupRepository.Verify(x => x.RemoveMembership(membership), Times.Once);
    }

    [Fact]
    public async Task Leave_NonMember_ShouldChangeNothing()
    {
        GivenMembership(null);

        var response = await new LeaveGroupHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new LeaveGroupRequest(_group.Id.Value));

        response.MemberCount.Should().Be(1);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Post_AsMember_ShouldCreatePost()
    {
        GivenMembership(GroupMembership.Create(_group.Id, _currentUserId));

        var response = await new CreateGroupPostHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new CreateGroupPostRequest(_group.Id.Value, "Dance on the beach Friday?"));

        response.Post.Body.Should().Be("Dance on the beach Friday?");
        response.Post.AuthorId.Should().Be(_currentUserId.Value);
        GroupRepository.Verify(x => x.AddPostAsync(It.IsAny<GroupPost>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Post_AsNonMember_ShouldThrowForbidden()
    {
        GivenMembership(null);

        var action = () => new CreateGroupPostHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new CreateGroupPostRequest(_group.Id.Value, "Hello"));

        await action.Should().ThrowAsync<ForbiddenException>();
        GroupRepository.Verify(x => x.AddPostAsync(It.IsAny<GroupPost>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Post_EmptyBody_ShouldThrowValidation()
    {
        var action = () => new CreateGroupPostHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new CreateGroupPostRequest(_group.Id.Value, "   "));

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Post_TooLongBody_ShouldThrowValidation()
    {
        var action = () => new CreateGroupPostHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new CreateGroupPostRequest(_group.Id.Value, new string('a', GroupPost.MaxBodyLength + 1)));

        await action.Should().ThrowAsync<ValidationException>();
    }
}
