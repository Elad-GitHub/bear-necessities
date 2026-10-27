using Moq;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Application.UseCases.Users.ReviewMembership;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.Tests.UseCases.Users;

public class ReviewMembershipHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly Mock<ICurrentUserService> _mockCurrentUser = new();
    private readonly ReviewMembershipHandler _handler;

    public ReviewMembershipHandlerTests()
    {
        _mockCurrentUser.Setup(x => x.HasRole(UserRole.Admin)).Returns(true);
        _handler = new ReviewMembershipHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object);
    }

    private User GivenPendingUser()
    {
        var user = User.Create("newbie@test.com", "Newbie");
        Mock.Get(_mockUnitOfWork.Object.Users)
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        return user;
    }

    [Fact]
    public async Task HandleAsync_Approve_ShouldApproveMembership()
    {
        var user = GivenPendingUser();

        var response = await _handler.HandleAsync(new ReviewMembershipRequest(user.Id.Value, Approve: true));

        response.MembershipStatus.Should().Be("Approved");
        user.MembershipStatus.Should().Be(MembershipStatus.Approved);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Reject_ShouldRejectMembership()
    {
        var user = GivenPendingUser();

        var response = await _handler.HandleAsync(new ReviewMembershipRequest(user.Id.Value, Approve: false));

        response.MembershipStatus.Should().Be("Rejected");
    }

    [Fact]
    public async Task HandleAsync_AsNonAdmin_ShouldThrowForbidden()
    {
        _mockCurrentUser.Setup(x => x.HasRole(UserRole.Admin)).Returns(false);

        var action = () => _handler.HandleAsync(new ReviewMembershipRequest(Guid.NewGuid(), Approve: true));

        await action.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task HandleAsync_MissingUser_ShouldThrowNotFound()
    {
        Mock.Get(_mockUnitOfWork.Object.Users)
            .Setup(x => x.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var action = () => _handler.HandleAsync(new ReviewMembershipRequest(Guid.NewGuid(), Approve: true));

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
