using Moq;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.UseCases.Feedback;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Tests.UseCases.Feedback;

public class FeedbackHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly Mock<ICurrentUserService> _mockCurrentUser = new();
    private readonly UserId _memberId = new(Guid.NewGuid());

    private SubmitFeedbackHandler SubmitHandler => new(_mockUnitOfWork.Object, _mockCurrentUser.Object);

    [Fact]
    public async Task Submit_SignedOut_ShouldStoreWithoutSenderAndKeepTheContactEmail()
    {
        FeedbackMessage? stored = null;
        Mock.Get(_mockUnitOfWork.Object.Feedback)
            .Setup(x => x.AddAsync(It.IsAny<FeedbackMessage>(), It.IsAny<CancellationToken>()))
            .Callback<FeedbackMessage, CancellationToken>((feedback, _) => stored = feedback);

        await SubmitHandler.HandleAsync(new SubmitFeedbackRequest("bug", "  The map is blank on my phone  ", "/map", "Safari", "me@example.com"));

        stored!.Kind.Should().Be(FeedbackKind.Bug);
        stored.Message.Should().Be("The map is blank on my phone");
        stored.SentBy.Should().BeNull();
        stored.ContactEmail.Should().Be("me@example.com");
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Submit_SignedIn_ShouldLinkTheSender()
    {
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUser.Setup(x => x.UserId).Returns(_memberId);
        FeedbackMessage? stored = null;
        Mock.Get(_mockUnitOfWork.Object.Feedback)
            .Setup(x => x.AddAsync(It.IsAny<FeedbackMessage>(), It.IsAny<CancellationToken>()))
            .Callback<FeedbackMessage, CancellationToken>((feedback, _) => stored = feedback);

        await SubmitHandler.HandleAsync(new SubmitFeedbackRequest("Idea", "A dark map at night please", null, null, null));

        stored!.SentBy.Should().Be(_memberId);
    }

    [Theory]
    [InlineData("complaint", "A real message here", null)]
    [InlineData("bug", "hi", null)]
    [InlineData("bug", "A real message here", "not-an-email")]
    public async Task Submit_InvalidInput_ShouldThrowValidation(string kind, string message, string? email)
    {
        var action = () => SubmitHandler.HandleAsync(new SubmitFeedbackRequest(kind, message, null, null, email));

        await action.Should().ThrowAsync<ValidationException>();
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOpen_NotAdmin_ShouldBeForbidden()
    {
        var action = () => new GetOpenFeedbackHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object)
            .HandleAsync(new GetOpenFeedbackRequest());

        await action.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_Admin_ShouldMarkTheMessageHandled()
    {
        var adminId = new UserId(Guid.NewGuid());
        _mockCurrentUser.Setup(x => x.HasRole(UserRole.Admin)).Returns(true);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminId);
        var feedback = FeedbackMessage.Create(FeedbackKind.Idea, "Add a dark mode", null, null, null, null);
        Mock.Get(_mockUnitOfWork.Object.Feedback)
            .Setup(x => x.GetByIdAsync(feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);

        await new HandleFeedbackHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object).HandleAsync(new HandleFeedbackRequest(feedback.Id));

        feedback.IsHandled.Should().BeTrue();
        feedback.HandledBy.Should().Be(adminId);
    }
}
