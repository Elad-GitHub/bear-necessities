namespace Nomadica.Application.Imports;

public sealed record ImportReport(int Inserted, int SkippedDuplicates, int Failed, IReadOnlyList<string> Errors)
{
    public static ImportReport Combine(IEnumerable<ImportReport> reports)
    {
        var all = reports.ToList();
        return new ImportReport(
            all.Sum(report => report.Inserted),
            all.Sum(report => report.SkippedDuplicates),
            all.Sum(report => report.Failed),
            all.SelectMany(report => report.Errors).ToList());
    }

    public override string ToString()
        => $"inserted={Inserted}, duplicates={SkippedDuplicates}, failed={Failed}"
           + (Errors.Count > 0 ? $"\nFirst errors:\n - {string.Join("\n - ", Errors.Take(10))}" : string.Empty);
}
