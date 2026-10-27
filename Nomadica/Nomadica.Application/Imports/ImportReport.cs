namespace Nomadica.Application.Imports;

public sealed record ImportReport(int Inserted, int SkippedDuplicates, int Failed, IReadOnlyList<string> Errors)
{
    public override string ToString()
        => $"inserted={Inserted}, duplicates={SkippedDuplicates}, failed={Failed}"
           + (Errors.Count > 0 ? $"\nFirst errors:\n - {string.Join("\n - ", Errors.Take(10))}" : string.Empty);
}
