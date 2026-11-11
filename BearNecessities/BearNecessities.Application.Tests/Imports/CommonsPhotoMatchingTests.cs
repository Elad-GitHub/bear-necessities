using BearNecessities.Application.Imports;

namespace BearNecessities.Application.Tests.Imports;

public class CommonsPhotoMatchingTests
{
    private static CommonsPhotoCandidate Candidate(string title, string license = "CC BY 2.0", string? artist = "Mahshidr") =>
        new($"File:{title}", $"https://upload.example/{title}", $"https://commons.example/{title}", artist, license);

    [Fact]
    public void PickPhoto_TitleHoldsEveryNameWord_ShouldPickItWithCredit()
    {
        var photo = CommonsPhotoMatching.PickPhoto("Mercado Municipal de Santo Amaro", "Lagos",
            [Candidate("Lagos harbour.jpg"), Candidate("Mercado Municipal de Santo Amaro em Lagos I - 18.03.2023.jpg")]);

        photo!.Url.Should().EndWith("Santo Amaro em Lagos I - 18.03.2023.jpg");
        photo.Credit.Should().Be("Photo: Mahshidr · CC BY 2.0");
    }

    [Theory]
    [InlineData("Mercado da Guia", "Guia Post Office, 1 September 2015.JPG")]
    [InlineData("Sagrada Familia Tantra Temple", "Temple Expiatori de la Sagrada Família (Barcelona) - 2.jpg")]
    [InlineData("Feira das Taipas", "Centro Pastoral de Caldas das Taipas - Portugal.jpg")]
    public void PickPhoto_TitleSharesOnlySomeNameWords_ShouldPickNothing(string placeName, string title)
    {
        CommonsPhotoMatching.PickPhoto(placeName, null, [Candidate(title)]).Should().BeNull();
    }

    [Fact]
    public void PickPhoto_NameIsOnlyGenericWords_ShouldPickNothing()
    {
        CommonsPhotoMatching.PickPhoto("Vegan Café", "Lisboa", [Candidate("Vegan cafe in Lisboa.jpg")]).Should().BeNull();
    }

    [Fact]
    public void PickPhoto_AccentsAndJoinersDiffer_ShouldStillMatch()
    {
        CommonsPhotoMatching.PickPhoto("Relva de Reboleira", null, [Candidate("Relva da Reboleira 02.jpg")]).Should().NotBeNull();
        CommonsPhotoMatching.PickPhoto("Jardim da Estrela", null, [Candidate("Jardim da Estrêla, Lisboa.jpg")]).Should().NotBeNull();
    }

    [Fact]
    public void PickPhoto_NonFreeLicenceOrNotAPhoto_ShouldBeSkipped()
    {
        var photo = CommonsPhotoMatching.PickPhoto("Cepo Verde", null,
            [Candidate("Cepo Verde logo.svg"), Candidate("Cepo Verde gate.jpg", license: "CC BY-NC 2.0")]);

        photo.Should().BeNull();
    }

    [Fact]
    public void PickPhoto_ArtistIsHtmlWithANote_ShouldCreditJustTheName()
    {
        var photo = CommonsPhotoMatching.PickPhoto("Cepo Verde", null,
            [Candidate("Cepo Verde Camping 001.jpg", artist: "<a href=\"//commons.wikimedia.org/wiki/User:Jo\">Jo</a> (talk)\nPlease mail me")]);

        photo!.Credit.Should().Be("Photo: Jo · CC BY 2.0");
    }

    [Fact]
    public void PickPhotoFromSearch_TitleNamesPlaceAndCity_ShouldPickIt()
    {
        var photo = CommonsPhotoMatching.PickPhotoFromSearch("Osho Meditation Resort", "Pune",
            [Candidate("Osho talk in Mumbai.jpg"), Candidate("2008 12 Osho center, Pune, India.jpg")]);

        photo!.Url.Should().EndWith("Osho center, Pune, India.jpg");
    }

    [Fact]
    public void PickPhotoFromSearch_TitleMissesTheCity_ShouldPickNothing()
    {
        CommonsPhotoMatching.PickPhotoFromSearch("Osho Meditation Resort", "Pune", [Candidate("Osho portrait.jpg")]).Should().BeNull();
    }
}
