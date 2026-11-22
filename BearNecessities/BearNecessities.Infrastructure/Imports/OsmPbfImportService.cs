using Microsoft.Extensions.Logging;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using BearNecessities.Application.Imports;
using BearNecessities.Application.Interfaces;

namespace BearNecessities.Infrastructure.Imports;

/// Imports places from a Geofabrik country extract (.osm.pbf) read locally, for when the public Overpass
/// servers rate-limit or block us. Uses the same category rules as the Overpass importer.
public sealed class OsmPbfImportService
{
    // Name rules only count on objects that are some kind of place, so a "Retreat Road" street never matches
    private static readonly string[] PlaceKeys =
        ["amenity", "leisure", "tourism", "shop", "healthcare", "club", "sport", "office", "craft", "building",
         "landuse", "natural", "cuisine"];

    private readonly IPlaceImportService _importer;
    private readonly ILogger<OsmPbfImportService> _logger;

    public OsmPbfImportService(IPlaceImportService importer, ILogger<OsmPbfImportService> logger)
    {
        _importer = importer;
        _logger = logger;
    }

    public async Task<ImportReport> ImportFileAsync(
        string pbfPath,
        string isoCountryCode,
        string? countryName,
        bool autoApprove,
        int maxResultsPerCategory = 300,
        CancellationToken cancellationToken = default)
    {
        var iso = isoCountryCode.ToUpperInvariant();
        var places = ReadPlaces(pbfPath, iso, countryName);

        // Same per-kind cap as the Overpass query, so thousands of cafés can't crowd out rare kinds
        var capped = places
            .GroupBy(place => place.CategorySlugs[0])
            .SelectMany(group => group.Take(maxResultsPerCategory))
            .ToList();

        _logger.LogInformation("{File}: {Count} usable places ({Capped} after the per-category cap)",
            Path.GetFileName(pbfPath), places.Count, capped.Count);
        return await _importer.ImportAsync(capped, autoApprove, cancellationToken);
    }

    public static List<ImportPlace> ReadPlaces(string pbfPath, string iso, string? countryName)
    {
        var candidates = FindCandidates(pbfPath);
        var wayNodeIds = ReadMemberWayNodes(pbfPath, candidates);
        var nodeCoordinates = ReadNodeCoordinates(pbfPath, candidates, wayNodeIds);
        return BuildPlaces(candidates, wayNodeIds, nodeCoordinates, iso, countryName);
    }

    public static List<ImportPlace> ReadPlaces(IEnumerable<OsmGeo> elements, string iso, string? countryName)
    {
        var list = elements as IList<OsmGeo> ?? elements.ToList();
        var candidates = FindCandidates(list);
        var wayNodeIds = ReadMemberWayNodes(list, candidates);
        var nodeCoordinates = ReadNodeCoordinates(list, candidates, wayNodeIds);
        return BuildPlaces(candidates, wayNodeIds, nodeCoordinates, iso, countryName);
    }

    private sealed class Candidates
    {
        public List<(long Id, Dictionary<string, string> Tags, decimal Latitude, decimal Longitude)> Nodes { get; } = [];
        public List<(long Id, Dictionary<string, string> Tags, long[] NodeIds)> Ways { get; } = [];
        public List<(long Id, Dictionary<string, string> Tags, long[] WayIds)> Relations { get; } = [];
    }

    private static Candidates FindCandidates(string pbfPath) => WithStream(pbfPath, FindCandidates);

    private static Candidates FindCandidates(IEnumerable<OsmGeo> elements)
    {
        var candidates = new Candidates();
        foreach (var element in elements)
        {
            if (element.Id is not { } id || !IsNamedPlace(element.Tags))
                continue;

            var tags = ToDictionary(element.Tags);
            if (OsmCategoryRules.Classify(tags).Count == 0)
                continue;

            switch (element)
            {
                case Node { Latitude: { } latitude, Longitude: { } longitude }:
                    candidates.Nodes.Add((id, tags, (decimal)latitude, (decimal)longitude));
                    break;
                case Way { Nodes.Length: > 0 } way:
                    candidates.Ways.Add((id, tags, way.Nodes));
                    break;
                case Relation { Members.Length: > 0 } relation:
                    var wayIds = relation.Members.Where(member => member.Type == OsmGeoType.Way)
                        .Select(member => member.Id).ToArray();
                    if (wayIds.Length > 0)
                        candidates.Relations.Add((id, tags, wayIds));
                    break;
            }
        }
        return candidates;
    }

    private static bool IsNamedPlace([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] TagsCollectionBase? tags) =>
        tags is not null && tags.ContainsKey("name") && PlaceKeys.Any(tags.ContainsKey);

    // Relations hold ways, not points, so their member ways' node lists are needed to place them
    private static Dictionary<long, long[]> ReadMemberWayNodes(string pbfPath, Candidates candidates) =>
        candidates.Relations.Count == 0
            ? CandidateWayNodes(candidates)
            : WithStream(pbfPath, elements => ReadMemberWayNodes(elements, candidates));

    private static Dictionary<long, long[]> ReadMemberWayNodes(IEnumerable<OsmGeo> elements, Candidates candidates)
    {
        var wayNodeIds = CandidateWayNodes(candidates);
        var memberWayIds = candidates.Relations.SelectMany(relation => relation.WayIds).ToHashSet();
        memberWayIds.ExceptWith(wayNodeIds.Keys);
        if (memberWayIds.Count == 0)
            return wayNodeIds;

        foreach (var element in elements)
        {
            if (element is Relation) break;
            if (element is Way { Id: { } id } way && memberWayIds.Contains(id))
                wayNodeIds[id] = way.Nodes;
        }
        return wayNodeIds;
    }

    private static Dictionary<long, long[]> CandidateWayNodes(Candidates candidates)
    {
        var wayNodeIds = new Dictionary<long, long[]>();
        foreach (var way in candidates.Ways)
            wayNodeIds[way.Id] = way.NodeIds;
        return wayNodeIds;
    }

    private static Dictionary<long, (decimal Latitude, decimal Longitude)> ReadNodeCoordinates(
        string pbfPath, Candidates candidates, Dictionary<long, long[]> wayNodeIds) =>
        wayNodeIds.Count == 0
            ? []
            : WithStream(pbfPath, elements => ReadNodeCoordinates(elements, candidates, wayNodeIds));

    private static Dictionary<long, (decimal Latitude, decimal Longitude)> ReadNodeCoordinates(
        IEnumerable<OsmGeo> elements, Candidates candidates, Dictionary<long, long[]> wayNodeIds)
    {
        var neededNodeIds = wayNodeIds.Values.SelectMany(nodeIds => nodeIds).ToHashSet();
        var coordinates = new Dictionary<long, (decimal, decimal)>();
        foreach (var element in elements)
        {
            // Extracts list every node before the first way, so the rest of the file can be skipped
            if (element is not Node node) break;
            if (node is { Id: { } id, Latitude: { } latitude, Longitude: { } longitude } && neededNodeIds.Contains(id))
                coordinates[id] = ((decimal)latitude, (decimal)longitude);
        }
        return coordinates;
    }

    private static List<ImportPlace> BuildPlaces(
        Candidates candidates,
        Dictionary<long, long[]> wayNodeIds,
        Dictionary<long, (decimal Latitude, decimal Longitude)> nodeCoordinates,
        string iso,
        string? countryName)
    {
        var places = new List<ImportPlace?>();
        foreach (var node in candidates.Nodes)
            places.Add(OsmPlaceMapper.ToImportPlace(node.Tags, "node", node.Id, node.Latitude, node.Longitude, iso, countryName));

        foreach (var way in candidates.Ways)
            if (TryGetCenter(way.NodeIds, nodeCoordinates, out var latitude, out var longitude))
                places.Add(OsmPlaceMapper.ToImportPlace(way.Tags, "way", way.Id, latitude, longitude, iso, countryName));

        foreach (var relation in candidates.Relations)
        {
            var nodeIds = relation.WayIds.SelectMany(wayId => wayNodeIds.GetValueOrDefault(wayId, []));
            if (TryGetCenter(nodeIds, nodeCoordinates, out var latitude, out var longitude))
                places.Add(OsmPlaceMapper.ToImportPlace(relation.Tags, "relation", relation.Id, latitude, longitude, iso, countryName));
        }

        return places.OfType<ImportPlace>().ToList();
    }

    // Bounding-box centre, the same point Overpass returns for "out center"
    private static bool TryGetCenter(
        IEnumerable<long> nodeIds,
        Dictionary<long, (decimal Latitude, decimal Longitude)> nodeCoordinates,
        out decimal latitude,
        out decimal longitude)
    {
        var points = nodeIds.Where(nodeCoordinates.ContainsKey).Select(nodeId => nodeCoordinates[nodeId]).ToList();
        latitude = 0;
        longitude = 0;
        if (points.Count == 0)
            return false;

        latitude = (points.Min(point => point.Latitude) + points.Max(point => point.Latitude)) / 2;
        longitude = (points.Min(point => point.Longitude) + points.Max(point => point.Longitude)) / 2;
        return true;
    }

    private static T WithStream<T>(string pbfPath, Func<IEnumerable<OsmGeo>, T> read)
    {
        using var stream = File.OpenRead(pbfPath);
        return read(new PBFOsmStreamSource(stream));
    }

    private static Dictionary<string, string> ToDictionary(TagsCollectionBase tags)
    {
        var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in tags)
            dictionary[tag.Key] = tag.Value;
        return dictionary;
    }
}
