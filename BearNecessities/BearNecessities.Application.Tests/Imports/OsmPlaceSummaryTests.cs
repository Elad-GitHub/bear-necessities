using BearNecessities.Application.Imports;

namespace BearNecessities.Application.Tests.Imports;

public class OsmPlaceSummaryTests
{
    private static Dictionary<string, string> Tags(params (string Key, string Value)[] pairs) =>
        pairs.ToDictionary(pair => pair.Key, pair => pair.Value);

    [Fact]
    public void Build_FullyVeganRestaurant_ShouldDescribeKindCuisineAndHours()
    {
        var summary = OsmPlaceSummary.Build(
            Tags(("amenity", "restaurant"), ("diet:vegan", "only"), ("cuisine", "indian;vegan"), ("opening_hours", "Mo-Sa 12:00-22:00")),
            "vegan-restaurant");

        summary.Should().Be("Fully vegan restaurant · Indian cuisine · Open Mo-Sa 12:00-22:00");
    }

    [Fact]
    public void Build_VegetarianCafe_ShouldNotRepeatTheCuisineItsKindAlreadyNames()
    {
        var summary = OsmPlaceSummary.Build(Tags(("amenity", "cafe"), ("cuisine", "vegetarian"), ("organic", "only")), "vegan-restaurant");

        summary.Should().Be("Vegetarian café · Organic");
    }

    [Fact]
    public void Build_Campsite_ShouldMentionTentsCaravansAndFreeEntry()
    {
        var summary = OsmPlaceSummary.Build(Tags(("tourism", "camp_site"), ("tents", "yes"), ("caravans", "yes"), ("fee", "no")), "camping");

        summary.Should().Be("Campsite · Tents & caravans · Free entry");
    }

    [Fact]
    public void Build_LongOpeningHours_ShouldBeLeftOut()
    {
        var summary = OsmPlaceSummary.Build(
            Tags(("opening_hours", "Mo-Fr 08:00-12:00,13:00-18:00; Sa 09:00-13:00; Su,PH off; Dec 24 closed")),
            "yoga");

        summary.Should().Be("Yoga studio");
    }

    [Fact]
    public void Build_IndianRestaurantWithAnotherCuisine_ShouldListItAsAnExtra()
    {
        var summary = OsmPlaceSummary.Build(Tags(("amenity", "restaurant"), ("cuisine", "indian;italian")), "indian-food");

        summary.Should().Be("Indian restaurant · Also Italian cuisine");
    }

    [Fact]
    public void Build_RestaurantWithVeganOptions_ShouldSaySo()
    {
        var summary = OsmPlaceSummary.Build(Tags(("amenity", "restaurant"), ("diet:vegan", "yes"), ("takeaway", "yes")), "vegan-restaurant");

        summary.Should().Be("Restaurant with vegan options · Takeaway");
    }
}
