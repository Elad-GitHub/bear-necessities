using System.Text.Json;
using Moq;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.Tests.Helpers;
using BearNecessities.Application.UseCases.EditSuggestions;
using BearNecessities.Application.UseCases.Places.UpdatePlace;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Tests.UseCases.EditSuggestions;

public class ReviewEditSuggestionHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new() { DefaultValue = DefaultValue.Mock };
    private readonly Mock<ICurrentUserService> _mockCurrentUser = new();
    private readonly UserId _adminId = new(Guid.NewGuid());
    private readonly UserId _memberId = new(Guid.NewGuid());
    private readonly ReviewEditSuggestionHandler _handler;

    public ReviewEditSuggestionHandlerTests()
    {
        _mockCurrentUser.Setup(x => x.UserId).Returns(_adminId);
        _mockCurrentUser.Setup(x => x.HasRole(UserRole.Admin)).Returns(true);
        _handler = new ReviewEditSuggestionHandler(_mockUnitOfWork.Object, _mockCurrentUser.Object, Mock.Of<IOccurrenceExpander>());
    }

    private (Place Place, EditSuggestion Suggestion) GivenSuggestedRename(string newName)
    {
        var place = TestDataBuilder.BuildPlace(_memberId, ListingStatus.Live);
        Mock.Get(_mockUnitOfWork.Object.Places)
            .Setup(x => x.GetByIdWithRelationsAsync(It.IsAny<PlaceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(place);

        var edit = new UpdatePlaceRequest(place.Id.Value, newName, null, null, null, null, null, null, null, null, null, null, null);
        var suggestion = EditSuggestion.Create(ListingChangeTargetType.Place, place.Id.Value, _memberId, JsonSerializer.Serialize(edit));
        Mock.Get(_mockUnitOfWork.Object.EditSuggestions)
            .Setup(x => x.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);
        return (place, suggestion);
    }

    [Fact]
    public async Task HandleAsync_Approve_ShouldApplyTheEditAndKeepThePlaceLive()
    {
        var (place, suggestion) = GivenSuggestedRename("Colibri Sanctuary");

        var response = await _handler.HandleAsync(new ReviewEditSuggestionRequest(suggestion.Id, IsApproved: true));

        response.Status.Should().Be("Approved");
        place.Name.Should().Be("Colibri Sanctuary");
        place.Status.Should().Be(ListingStatus.Live);
        suggestion.ReviewedBy.Should().Be(_adminId);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Reject_ShouldLeaveThePlaceAsIs()
    {
        var (place, suggestion) = GivenSuggestedRename("Colibri Sanctuary");
        var originalName = place.Name;

        var response = await _handler.HandleAsync(new ReviewEditSuggestionRequest(suggestion.Id, IsApproved: false));

        response.Status.Should().Be("Rejected");
        place.Name.Should().Be(originalName);
    }

    [Fact]
    public async Task HandleAsync_AlreadyReviewed_ShouldThrowValidation()
    {
        var (_, suggestion) = GivenSuggestedRename("Colibri Sanctuary");
        suggestion.Reject(_adminId);

        var action = () => _handler.HandleAsync(new ReviewEditSuggestionRequest(suggestion.Id, IsApproved: true));

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_NotAdmin_ShouldThrowForbidden()
    {
        _mockCurrentUser.Setup(x => x.HasRole(UserRole.Admin)).Returns(false);
        var (_, suggestion) = GivenSuggestedRename("Colibri Sanctuary");

        var action = () => _handler.HandleAsync(new ReviewEditSuggestionRequest(suggestion.Id, IsApproved: true));

        await action.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task HandleAsync_ApproveCityOnly_ShouldKeepAddressAndCountry()
    {
        var place = TestDataBuilder.BuildPlace(_memberId, ListingStatus.Live);
        place.SetAddress("Main road 1", "Arillas", "Greece", "GR");
        Mock.Get(_mockUnitOfWork.Object.Places)
            .Setup(x => x.GetByIdWithRelationsAsync(It.IsAny<PlaceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(place);
        var edit = new UpdatePlaceRequest(place.Id.Value, null, null, null, null, null, "Agios Stefanos", null, null, null, null, null, null);
        var suggestion = EditSuggestion.Create(ListingChangeTargetType.Place, place.Id.Value, _memberId, JsonSerializer.Serialize(edit));
        Mock.Get(_mockUnitOfWork.Object.EditSuggestions)
            .Setup(x => x.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        await _handler.HandleAsync(new ReviewEditSuggestionRequest(suggestion.Id, IsApproved: true));

        place.City.Should().Be("Agios Stefanos");
        place.Address.Should().Be("Main road 1");
        place.Country.Should().Be("Greece");
        place.CountryCode.Should().Be("GR");
    }

    [Fact]
    public void PlaceDiff_ShouldListOnlyTheFieldsThatChange()
    {
        var place = TestDataBuilder.BuildPlace(_memberId, ListingStatus.Live);
        var edit = new UpdatePlaceRequest(place.Id.Value, place.Name, "Sea views and a sauna", null, null, null, null, null, null, null, null, null, null);

        var changes = EditSuggestionDiff.ForPlace(place, edit, new Dictionary<Guid, string>());

        changes.Should().ContainSingle().Which.Should().Be(new FieldChangeDto("Description", place.Description, "Sea views and a sauna"));
    }
}
