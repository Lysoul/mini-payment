using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using FluentValidation;
using MiniPayment.Reconciliation.Models;
using MiniPayment.Reconciliation.Validation;
using Serilog;

namespace MiniPayment.Reconciliation.Matching;

/// <summary>
/// Two-pass reconciliation orchestrator. RunAsync composes three phases:
/// load B into a keyed dictionary, stream A while classifying each row against
/// that dictionary, and flush whatever keys remain in the dictionary as
/// Missing_In_A. Each phase is independently testable.
/// </summary>
public static class ReconciliationEngine
{
    private static readonly IValidator<ListARecord> ValidatorA = new ListARecordValidator();
    private static readonly IValidator<ListBRecord> ValidatorB = new ListBRecordValidator();

    private static CsvConfiguration CsvConfig => new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        TrimOptions = TrimOptions.Trim,
        MissingFieldFound = null
    };

    public static async Task<ReconciliationReport> RunAsync(
        string listAPath,
        string listBPath,
        string outputDir,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDir);
        await using var sinks = ReconciliationSinks.Open(outputDir, CsvConfig);

        Log.Information("Phase 1: Loading List B references into memory...");
        var (bRecords, bStats) = await LoadListBAsync(listBPath, sinks, ct);
        Log.Information("List B: {Valid} valid, {Duplicates} duplicates, {Rejected} rejected",
            bStats.Valid, bStats.Duplicates, bStats.Rejected);

        Log.Information("Phase 2: Streaming List A and matching...");
        var aStats = await StreamAndClassifyAsync(listAPath, bRecords, sinks, ct);

        Log.Information("Phase 3: Writing {Count} leftover B records to Missing_In_A...", bRecords.Count);
        await WriteMissingInAAsync(bRecords, sinks, ct);

        var report = new ReconciliationReport(
            Matched:          aStats.Matched,
            MissingInA:       bRecords.Count,
            MissingInB:       aStats.MissingInB,
            DuplicatesA:      aStats.Duplicates,
            DuplicatesB:      bStats.Duplicates,
            RejectedA:        aStats.Rejected,
            RejectedB:        bStats.Rejected,
            OutputDirectory:  outputDir);

        Log.Information(
            "Reconciliation complete. Matched: {Matched}, Missing in B: {MissingInB}, Missing in A: {MissingInA}, Duplicates A: {DuplicatesA}, Duplicates B: {DuplicatesB}, Rejected A: {RejectedA}, Rejected B: {RejectedB}",
            report.Matched, report.MissingInB, report.MissingInA,
            report.DuplicatesA, report.DuplicatesB, report.RejectedA, report.RejectedB);

        return report;
    }

    private static async Task<(Dictionary<string, ListBRecord> Records, LoadStats Stats)> LoadListBAsync(
        string path,
        ReconciliationSinks sinks,
        CancellationToken ct)
    {
        var records = new Dictionary<string, ListBRecord>(StringComparer.OrdinalIgnoreCase);
        int rejected = 0, duplicates = 0;

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CsvConfig);

        await foreach (var record in csv.GetRecordsAsync<ListBRecord>().WithCancellation(ct))
        {
            var validation = ValidatorB.Validate(record);
            if (!validation.IsValid)
            {
                rejected++;
                var reasons = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                sinks.RejectedB.WriteRecord(new RejectedRow(record.InvoiceNumber, reasons));
                sinks.RejectedB.NextRecord();
                Log.Warning("List B row rejected: {InvoiceNumber} — {Reasons}", record.InvoiceNumber, reasons);
                continue;
            }
            if (records.ContainsKey(record.InvoiceNumber))
            {
                duplicates++;
                sinks.DuplicatesB.WriteRecord(record);
                sinks.DuplicatesB.NextRecord();
                Log.Warning("List B duplicate InvoiceNumber (keeping first occurrence): {InvoiceNumber}", record.InvoiceNumber);
                continue;
            }
            records[record.InvoiceNumber] = record;
        }

        return (records, new LoadStats(records.Count, duplicates, rejected));
    }

    private static async Task<MatchStats> StreamAndClassifyAsync(
        string path,
        Dictionary<string, ListBRecord> bRecords,
        ReconciliationSinks sinks,
        CancellationToken ct)
    {
        int matched = 0, missingInB = 0, rejected = 0, duplicates = 0;
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CsvConfig);

        await foreach (var record in csv.GetRecordsAsync<ListARecord>().WithCancellation(ct))
        {
            var validation = ValidatorA.Validate(record);
            if (!validation.IsValid)
            {
                rejected++;
                var reasons = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                sinks.RejectedA.WriteRecord(new RejectedRow(record.OrderNumber, reasons));
                sinks.RejectedA.NextRecord();
                Log.Warning("List A row rejected: {OrderNumber} — {Reasons}", record.OrderNumber, reasons);
                continue;
            }

            if (!seenKeys.Add(record.OrderNumber))
            {
                duplicates++;
                sinks.DuplicatesA.WriteRecord(record);
                sinks.DuplicatesA.NextRecord();
                Log.Warning("List A duplicate OrderNumber (keeping first occurrence): {OrderNumber}", record.OrderNumber);
                continue;
            }

            if (bRecords.TryGetValue(record.OrderNumber, out var bRecord))
            {
                matched++;
                sinks.Matched.WriteRecord(new MatchedRow(
                    record.OrderNumber,
                    record.Date, record.Amount,
                    bRecord.Date, bRecord.Amount));
                sinks.Matched.NextRecord();
                bRecords.Remove(record.OrderNumber);
            }
            else
            {
                missingInB++;
                sinks.MissingInB.WriteRecord(record);
                sinks.MissingInB.NextRecord();
            }
        }

        return new MatchStats(matched, missingInB, duplicates, rejected);
    }

    private static Task WriteMissingInAAsync(
        Dictionary<string, ListBRecord> remainingB,
        ReconciliationSinks sinks,
        CancellationToken ct)
    {
        foreach (var remaining in remainingB.Values)
        {
            ct.ThrowIfCancellationRequested();
            sinks.MissingInA.WriteRecord(remaining);
            sinks.MissingInA.NextRecord();
        }
        return Task.CompletedTask;
    }

    private sealed record LoadStats(int Valid, int Duplicates, int Rejected);
    private sealed record MatchStats(int Matched, int MissingInB, int Duplicates, int Rejected);
}
