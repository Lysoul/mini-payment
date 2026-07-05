namespace MiniPayment.Reconciliation.Matching;

/// <summary>
/// Result of a reconciliation run. All counts refer to rows written to the
/// corresponding output file. Callers should prefer this over parsing log lines.
/// </summary>
public sealed record ReconciliationReport(
    int Matched,
    int MissingInA,
    int MissingInB,
    int DuplicatesA,
    int DuplicatesB,
    int RejectedA,
    int RejectedB,
    string OutputDirectory);
