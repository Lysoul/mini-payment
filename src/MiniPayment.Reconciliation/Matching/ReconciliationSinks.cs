using CsvHelper;
using CsvHelper.Configuration;
using MiniPayment.Reconciliation.Models;

namespace MiniPayment.Reconciliation.Matching;

internal sealed record RejectedRow(string Reference, string Reason);

internal sealed record MatchedRow(
    string Reference,
    string A_Date, string A_Amount,
    string B_Date, string B_Amount);

/// <summary>
/// Owns every CSV writer the reconciliation pipeline emits to. Callers get
/// typed access to each sink and a single DisposeAsync() that flushes them all,
/// so phase methods never touch StreamWriter or file paths directly.
/// </summary>
internal sealed class ReconciliationSinks : IAsyncDisposable
{
    public CsvWriter Matched { get; }
    public CsvWriter MissingInA { get; }
    public CsvWriter MissingInB { get; }
    public CsvWriter DuplicatesA { get; }
    public CsvWriter DuplicatesB { get; }
    public CsvWriter RejectedA { get; }
    public CsvWriter RejectedB { get; }

    private readonly CsvWriter[] _all;

    private ReconciliationSinks(
        CsvWriter matched, CsvWriter missingInA, CsvWriter missingInB,
        CsvWriter duplicatesA, CsvWriter duplicatesB,
        CsvWriter rejectedA, CsvWriter rejectedB)
    {
        Matched = matched;
        MissingInA = missingInA;
        MissingInB = missingInB;
        DuplicatesA = duplicatesA;
        DuplicatesB = duplicatesB;
        RejectedA = rejectedA;
        RejectedB = rejectedB;
        _all = [matched, missingInA, missingInB, duplicatesA, duplicatesB, rejectedA, rejectedB];
    }

    public static ReconciliationSinks Open(string outputDir, CsvConfiguration csvConfig) =>
        new(
            matched:     OpenWithHeader(outputDir, "Matched_Records.csv", csvConfig, typeof(MatchedRow)),
            missingInA:  OpenWithHeader(outputDir, "Missing_In_A.csv",    csvConfig, typeof(ListBRecord)),
            missingInB:  OpenWithHeader(outputDir, "Missing_In_B.csv",    csvConfig, typeof(ListARecord)),
            duplicatesA: OpenWithHeader(outputDir, "Duplicates_A.csv",    csvConfig, typeof(ListARecord)),
            duplicatesB: OpenWithHeader(outputDir, "Duplicates_B.csv",    csvConfig, typeof(ListBRecord)),
            rejectedA:   OpenWithHeader(outputDir, "Rejected_A.csv",      csvConfig, typeof(RejectedRow)),
            rejectedB:   OpenWithHeader(outputDir, "Rejected_B.csv",      csvConfig, typeof(RejectedRow)));

    private static CsvWriter OpenWithHeader(string dir, string fileName, CsvConfiguration cfg, Type recordType)
    {
        var sw = new StreamWriter(Path.Combine(dir, fileName));
        var csv = new CsvWriter(sw, cfg);
        csv.WriteHeader(recordType);
        csv.NextRecord();
        return csv;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var w in _all)
        {
            await w.DisposeAsync();
        }
    }
}
