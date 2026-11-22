using OsmSharp;
using OsmSharp.Tags;
using BearNecessities.Infrastructure.Imports;

namespace BearNecessities.Infrastructure.Tests.Imports;

public class OsmPbfImportServiceTests
{
    private static Node Point(long id, double latitude, double longitude, params (string Key, string Value)[] tags) =>
        new() { Id = id, Latitude = latitude, Longitude = longitude, Tags = Tags(tags) };

    private static TagsCollection Tags((string Key, string Value)[] tags) =>
        new(tags.Select(tag => new Tag(tag.Key, tag.Value)));

    [Fact]
    public void ReadPlaces_TaggedNode_ShouldBecomePlaceWithItsCategory()
    {
        var places = OsmPbfImportService.ReadPlaces(
            [Point(1, 38.7, -9.1, ("name", "Lisbon Yoga"), ("sport", "yoga"))], "pt", "Portugal");

        places.Should().ContainSingle();
        places[0].CategorySlugs.Should().Contain("yoga");
        places[0].ExternalSourceId.Should().Be("osm:node/1");
        places[0].CountryCode.Should().Be("PT");
    }

    [Fact]
    public void ReadPlaces_UnrelatedOrUnnamedNodes_ShouldBeSkipped()
    {
        var places = OsmPbfImportService.ReadPlaces(
        [
            Point(1, 38.7, -9.1, ("name", "Corner Bank"), ("amenity", "bank")),
            Point(2, 38.7, -9.1, ("sport", "yoga")),
            Point(3, 38.7, -9.1, ("name", "Retreat Road"), ("highway", "residential"))
        ], "PT", null);

        places.Should().BeEmpty();
    }

    [Fact]
    public void ReadPlaces_TaggedWay_ShouldSitAtTheCentreOfItsNodes()
    {
        var places = OsmPbfImportService.ReadPlaces(
        [
            Point(10, 38.0, -9.0),
            Point(11, 39.0, -8.0),
            new Way { Id = 5, Nodes = [10, 11], Tags = Tags([("name", "Quinta Camp"), ("tourism", "camp_site")]) }
        ], "PT", null);

        places.Should().ContainSingle();
        places[0].Latitude.Should().Be(38.5m);
        places[0].Longitude.Should().Be(-8.5m);
        places[0].ExternalSourceId.Should().Be("osm:way/5");
    }

    [Fact]
    public void ReadPlaces_TaggedRelation_ShouldUseItsMemberWays()
    {
        var places = OsmPbfImportService.ReadPlaces(
        [
            Point(10, 10.0, 20.0),
            Point(11, 12.0, 22.0),
            new Way { Id = 7, Nodes = [10, 11] },
            new Relation
            {
                Id = 3,
                Members = [new RelationMember(7, "outer", OsmGeoType.Way)],
                Tags = Tags([("name", "Garden Collective"), ("leisure", "garden"), ("garden:type", "community")])
            }
        ], "PT", null);

        places.Should().ContainSingle();
        places[0].Latitude.Should().Be(11m);
        places[0].Longitude.Should().Be(21m);
        places[0].CategorySlugs.Should().Contain("community-garden");
    }
}
