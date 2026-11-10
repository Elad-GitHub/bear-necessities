using System.Globalization;
using Microsoft.Extensions.Logging;
using BearNecessities.Application.Imports;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Enums;

namespace BearNecessities.Infrastructure.Imports;

/// Imports a curated CSV of places. Expected header (case-insensitive):
/// Name,Latitude,Longitude,Categories,City,Country,Website,Description
/// Categories is a semicolon-separated list of category slugs (e.g. "ecstatic-dance;sound-healing").
public sealed class CsvImportService
{
    private readonly IPlaceImportService _importer;
    private readonly ILogger<CsvImportService> _logger;

    public CsvImportService(IPlaceImportService importer, ILogger<CsvImportService> logger)
    {
        _importer = importer;
        _logger = logger;
    }

    public async Task<ImportReport> ImportFileAsync(
        string filePath,
        bool autoApprove,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return new ImportReport(0, 0, 0, [$"File not found: {filePath}"]);

        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        if (lines.Length < 2)
            return new ImportReport(0, 0, 0, ["CSV has no data rows."]);

        var header = ParseLine(lines[0]);
        var columns = header
            .Select((name, index) => (name: name.Trim().ToLowerInvariant(), index))
            .ToDictionary(pair => pair.name, pair => pair.index);

        var places = new List<ImportPlace>();
        var errors = new List<string>();

        for (var row = 1; row < lines.Length; row++)
        {
            if (string.IsNullOrWhiteSpace(lines[row])) continue;

            try
            {
                var fields = ParseLine(lines[row]);
                var place = MapRow(fields, columns);
                if (place is not null) places.Add(place);
            }
            catch (Exception ex)
            {
                errors.Add($"Row {row + 1}: {ex.Message}");
            }
        }

        var report = await _importer.ImportAsync(places, autoApprove, cancellationToken);
        return errors.Count == 0
            ? report
            : report with { Errors = report.Errors.Concat(errors).ToList() };
    }

    private static ImportPlace? MapRow(IReadOnlyList<string> fields, IReadOnlyDictionary<string, int> columns)
    {
        var name = Field(fields, columns, "name");
        var latText = Field(fields, columns, "latitude");
        var lngText = Field(fields, columns, "longitude");

        if (string.IsNullOrWhiteSpace(name)) return null;
        if (!decimal.TryParse(latText, NumberStyles.Any, CultureInfo.InvariantCulture, out var latitude)) return null;
        if (!decimal.TryParse(lngText, NumberStyles.Any, CultureInfo.InvariantCulture, out var longitude)) return null;

        var categorySlugs = (Field(fields, columns, "categories") ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(slug => slug.ToLowerInvariant())
            .ToList();

        return new ImportPlace(
            Name: name!,
            Latitude: latitude,
            Longitude: longitude,
            CategorySlugs: categorySlugs,
            Source: ListingSourceType.User,
            ExternalSourceId: null,
            Description: Field(fields, columns, "description"),
            City: Field(fields, columns, "city"),
            Country: Field(fields, columns, "country"),
            Website: Field(fields, columns, "website"),
            IsPermanent: true);
    }

    private static string? Field(IReadOnlyList<string> fields, IReadOnlyDictionary<string, int> columns, string name)
    {
        if (!columns.TryGetValue(name, out var index) || index >= fields.Count) return null;
        var value = fields[index].Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    // Minimal RFC-4180-ish parser: handles quoted fields and escaped quotes
    private static List<string> ParseLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (inQuotes)
            {
                if (character == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else inQuotes = false;
                }
                else current.Append(character);
            }
            else if (character == '"') inQuotes = true;
            else if (character == ',') { fields.Add(current.ToString()); current.Clear(); }
            else current.Append(character);
        }

        fields.Add(current.ToString());
        return fields;
    }
}
