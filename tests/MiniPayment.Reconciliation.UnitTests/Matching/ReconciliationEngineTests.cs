using FluentAssertions;
using MiniPayment.Reconciliation.Matching;

namespace MiniPayment.Reconciliation.UnitTests.Matching;

public class ReconciliationEngineTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string _outputDir;

    public ReconciliationEngineTests()
    {
        Directory.CreateDirectory(_tempDir);
        _outputDir = Path.Combine(_tempDir, "output");
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public async Task Matched_And_Missing_FilesAreGenerated()
    {
        var listA = WriteCsv("listA.csv",
            "#,Order Number,Transaction Date,Amount,Status",
            "1,ORD-001,2024-01-01,100.00,Success",
            "2,ORD-002,2024-01-02,200.00,Success",
            "3,ORD-003,2024-01-03,300.00,Success");

        var listB = WriteCsv("listB.csv",
            "#,Invoice Number,Transaction Date,Amount,Status",
            "1,ORD-001,01-01-2024,100.00,Success",   // match
            "2,ORD-999,09-01-2024,999.00,Success");  // missing in A

        await ReconciliationEngine.RunAsync(listA, listB, _outputDir);

        var matched = ReadCsv(Path.Combine(_outputDir, "Matched_Records.csv"));
        matched.Should().ContainSingle(r => r.Contains("ORD-001"));

        var missingInB = ReadCsv(Path.Combine(_outputDir, "Missing_In_B.csv"));
        missingInB.Should().Contain(r => r.Contains("ORD-002"));
        missingInB.Should().Contain(r => r.Contains("ORD-003"));

        var missingInA = ReadCsv(Path.Combine(_outputDir, "Missing_In_A.csv"));
        missingInA.Should().ContainSingle(r => r.Contains("ORD-999"));
    }

    [Fact]
    public async Task InvalidRows_AreWrittenToRejectedFiles()
    {
        var listA = WriteCsv("listA_bad.csv",
            "#,Order Number,Transaction Date,Amount,Status",
            "1,,2024-01-01,100.00,Success",           // empty order number → rejected
            "2,ORD-OK,2024-01-02,200.00,Success");

        var listB = WriteCsv("listB_bad.csv",
            "#,Invoice Number,Transaction Date,Amount,Status",
            "1,INV-001,notadate,abc,Success");        // bad date + bad amount → rejected

        await ReconciliationEngine.RunAsync(listA, listB, _outputDir);

        var rejA = ReadCsv(Path.Combine(_outputDir, "Rejected_A.csv"));
        rejA.Should().HaveCountGreaterThan(0);

        var rejB = ReadCsv(Path.Combine(_outputDir, "Rejected_B.csv"));
        rejB.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task DuplicateKeys_AreWrittenToDuplicatesFiles_AndFirstOccurrenceIsMatched()
    {
        var listA = WriteCsv("listA_dup.csv",
            "#,Order Number,Transaction Date,Amount,Status",
            "1,ORD-001,2024-01-01,100.00,Success",   // first: matches B
            "2,ORD-001,2024-01-02,111.00,Success",   // duplicate in A
            "3,ORD-002,2024-01-03,200.00,Success");

        var listB = WriteCsv("listB_dup.csv",
            "#,Invoice Number,Transaction Date,Amount,Status",
            "1,ORD-001,01-01-2024,100.00,Success",   // first: matched by A
            "2,ORD-003,01-01-2024,300.00,Success",   // first occurrence, ends up Missing_In_A
            "3,ORD-003,02-01-2024,301.00,Success");  // duplicate in B

        await ReconciliationEngine.RunAsync(listA, listB, _outputDir);

        var matched = ReadCsv(Path.Combine(_outputDir, "Matched_Records.csv"));
        matched.Should().ContainSingle(r => r.Contains("ORD-001"));

        var dupA = ReadCsv(Path.Combine(_outputDir, "Duplicates_A.csv"));
        dupA.Should().ContainSingle(r => r.Contains("ORD-001") && r.Contains("111.00"));

        var dupB = ReadCsv(Path.Combine(_outputDir, "Duplicates_B.csv"));
        dupB.Should().ContainSingle(r => r.Contains("ORD-003") && r.Contains("301.00"));

        var missingInB = ReadCsv(Path.Combine(_outputDir, "Missing_In_B.csv"));
        missingInB.Should().ContainSingle(r => r.Contains("ORD-002"));

        var missingInA = ReadCsv(Path.Combine(_outputDir, "Missing_In_A.csv"));
        missingInA.Should().ContainSingle(r => r.Contains("ORD-003") && r.Contains("300.00"));
    }

    [Fact]
    public async Task ExtraSourceColumns_ArePreservedInOutputs()
    {
        var listA = WriteCsv("listA_extra.csv",
            "#,Order Number,Transaction Date,Amount,Fees1,Fees2,Net Total,Status",
            "1,ORD-777,2024-01-01,\"3,900.00\",-0.8,-21.3,\"3,877.90\",Success");

        var listB = WriteCsv("listB_extra.csv",
            "#,Invoice Number,Transaction Date,Amount,Fees1,Fees2,Net Total,Card Number,Status",
            "1,ORD-888,01-01-2024,\"500.00\",-0.8,-5.0,\"494.20\",555555****1111,Success");

        await ReconciliationEngine.RunAsync(listA, listB, _outputDir);

        var missingInB = File.ReadAllText(Path.Combine(_outputDir, "Missing_In_B.csv"));
        missingInB.Should().Contain("Fees1").And.Contain("Net Total").And.Contain("3,877.90");

        var missingInA = File.ReadAllText(Path.Combine(_outputDir, "Missing_In_A.csv"));
        missingInA.Should().Contain("Fees1").And.Contain("Net Total").And.Contain("494.20");
    }

    [Fact]
    public async Task RunAsync_ReturnsReport_WithAccurateCounts()
    {
        var listA = WriteCsv("listA_report.csv",
            "#,Order Number,Transaction Date,Amount,Status",
            "1,ORD-001,2024-01-01,100.00,Success",   // matched
            "2,ORD-002,2024-01-02,200.00,Success",   // missing_in_B
            "3,,2024-01-03,300.00,Success");         // rejected (empty key)

        var listB = WriteCsv("listB_report.csv",
            "#,Invoice Number,Transaction Date,Amount,Status",
            "1,ORD-001,01-01-2024,100.00,Success",   // matched (with A)
            "2,ORD-999,09-01-2024,999.00,Success",   // missing_in_A
            "3,ORD-999,10-01-2024,888.00,Success");  // duplicate_B

        var report = await ReconciliationEngine.RunAsync(listA, listB, _outputDir);

        report.Matched.Should().Be(1);
        report.MissingInB.Should().Be(1);
        report.MissingInA.Should().Be(1);
        report.DuplicatesA.Should().Be(0);
        report.DuplicatesB.Should().Be(1);
        report.RejectedA.Should().Be(1);
        report.RejectedB.Should().Be(0);
        report.OutputDirectory.Should().Be(_outputDir);
    }

    [Fact]
    public async Task EmptyFiles_ProduceEmptyOutputs()
    {
        var listA = WriteCsv("emptyA.csv", "#,Order Number,Transaction Date,Amount,Status");
        var listB = WriteCsv("emptyB.csv", "#,Invoice Number,Transaction Date,Amount,Status");

        await ReconciliationEngine.RunAsync(listA, listB, _outputDir);

        ReadCsv(Path.Combine(_outputDir, "Matched_Records.csv")).Should().HaveCount(0);
        ReadCsv(Path.Combine(_outputDir, "Missing_In_B.csv")).Should().HaveCount(0);
        ReadCsv(Path.Combine(_outputDir, "Missing_In_A.csv")).Should().HaveCount(0);
    }

    private string WriteCsv(string name, params string[] lines)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllLines(path, lines);
        return path;
    }

    private static List<string> ReadCsv(string path)
    {
        if (!File.Exists(path)) return [];
        return File.ReadAllLines(path)
            .Skip(1)                  // skip header
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();
    }
}
