using BearNecessities.Application.Imports;

namespace BearNecessities.Application.Tests.Imports;

public class OsmCategoryRulesTests
{
    private static IReadOnlyList<string> Classify(params (string Key, string Value)[] tags) =>
        OsmCategoryRules.Classify(tags.ToDictionary(tag => tag.Key, tag => tag.Value));

    [Fact]
    public void Classify_FoodPlaceNamedTantra_ShouldNotBeTantra()
    {
        Classify(("name", "Tantra Beach Stay"), ("amenity", "restaurant"), ("cuisine", "indian"))
            .Should().Equal("indian-food");
    }

    [Fact]
    public void Classify_VeganIndianRestaurant_ShouldHaveBoth()
    {
        Classify(("name", "Shanti Kitchen"), ("amenity", "restaurant"), ("cuisine", "indian;vegan"))
            .Should().Equal("vegan-restaurant", "indian-food");
    }

    [Theory]
    [InlineData("Dhamma Giri", "vipassana")]
    [InlineData("Vipassana Meditation Centre", "vipassana")]
    [InlineData("Osho Meditation Resort", "meditation")]
    [InlineData("Sunrise Yoga Retreat", "retreat")]
    [InlineData("Lakshmi Ayurveda Retreat", "ayurveda")]
    [InlineData("Ecovila Terra Mater", "eco-village")]
    [InlineData("Casa Temazcal", "sweat-lodge")]
    public void Classify_PracticeInTheName_ShouldBeRecognised(string name, string expected)
    {
        Classify(("name", name), ("tourism", "guest_house")).Should().Contain(expected);
    }

    [Theory]
    [InlineData("Mercado Hippie de Es Canar", "hippie-market")]
    [InlineData("Feira de Artesanato", "artisan-market")]
    [InlineData("Mercado Municipal de Lagos", "farmers-market")]
    public void Classify_Market_ShouldPickTheKindFromTheName(string name, string expected)
    {
        Classify(("name", name), ("amenity", "marketplace")).Should().Equal(expected);
    }

    [Fact]
    public void Classify_AlternativeHealthcare_ShouldReadItsSpecialities()
    {
        Classify(("name", "Healing House"), ("healthcare", "alternative"), ("healthcare:speciality", "ayurveda;reiki"))
            .Should().Equal("ayurveda", "reiki");
    }

    [Theory]
    [InlineData("Lek Massage", "thai", "thai-massage")]
    [InlineData("Thai Spa & Massage", null, "thai-massage")]
    [InlineData("Lisbon Massage Studio", null, "massage")]
    public void Classify_MassageShop_ShouldTellThaiMassageApart(string name, string? style, string expected)
    {
        var tags = new List<(string, string)> { ("name", name), ("shop", "massage") };
        if (style is not null) tags.Add(("massage", style));
        Classify(tags.ToArray()).Should().Contain(expected);
    }

    [Fact]
    public void Classify_OnsenPublicBath_ShouldBeHotSprings()
    {
        Classify(("name", "Hot Water Beach Onsen"), ("amenity", "public_bath"), ("bath:type", "onsen"))
            .Should().Contain("hot-springs");
    }

    [Fact]
    public void Classify_PlaceWithNothingRelevant_ShouldBeEmpty()
    {
        Classify(("name", "Corner Supermarket"), ("shop", "supermarket")).Should().BeEmpty();
    }

    [Fact]
    public void Classify_ManyMatches_ShouldKeepAtMostFive()
    {
        Classify(("name", "Yoga Ayurveda Reiki Retreat Kirtan Temazcal Eco Village"), ("sport", "yoga"))
            .Should().HaveCount(OsmCategoryRules.MaxCategories);
    }
}
