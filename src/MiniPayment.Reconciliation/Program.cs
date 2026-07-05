using MiniPayment.Reconciliation.Matching;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    string? listA = null, listB = null, outputDir = "./output";

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--list-a" when i + 1 < args.Length: listA = args[++i]; break;
            case "--list-b" when i + 1 < args.Length: listB = args[++i]; break;
            case "--output-dir" when i + 1 < args.Length: outputDir = args[++i]; break;
        }
    }

    if (listA is null || listB is null)
    {
        Console.Error.WriteLine("Usage: MiniPayment.Reconciliation --list-a <path> --list-b <path> [--output-dir <dir>]");
        return 1;
    }

    if (!File.Exists(listA)) { Log.Error("List A file not found: {Path}", listA); return 1; }
    if (!File.Exists(listB)) { Log.Error("List B file not found: {Path}", listB); return 1; }

    Log.Information("Starting reconciliation: A={ListA} B={ListB} Output={OutputDir}", listA, listB, outputDir);

    await ReconciliationEngine.RunAsync(listA, listB, outputDir);
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Reconciliation failed");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
