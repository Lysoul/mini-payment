using System.Text.RegularExpressions;
using Destructurama;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace MiniPayment.Infrastructure.Logging;

public static partial class LoggingRegistration
{
    [GeneratedRegex(@"\b\d{13,19}\b")]
    private static partial Regex PanPattern();

    public static void ConfigureSerilog(HostBuilderContext ctx, LoggerConfiguration config)
    {
        config
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Destructure.UsingAttributes()
            // Belt-and-braces: replace any 13–19 digit run in any string scalar
            .Destructure.ByTransforming<string>(s =>
                PanPattern().Replace(s, m => MaskPan(m.Value)))
            .WriteTo.Console(outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} | {Level:u5} | {CorrelationId,-36} | {Message:lj}{NewLine}{Exception}");
    }

    private static string MaskPan(string pan)
    {
        if (pan.Length < 6) return "***";
        return pan[..6] + new string('*', pan.Length - 10) + pan[^4..];
    }
}
