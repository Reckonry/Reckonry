using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LedgerForge.Core;

namespace LedgerForge.Reports;

public sealed class LedgerReportWriter : ILedgerReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    static LedgerReportWriter()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task WriteAsync(
        string ledgerJsonPath,
        IReadOnlyCollection<LedgerEvent> events,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ledgerJsonPath);
        ArgumentNullException.ThrowIfNull(events);

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(ledgerJsonPath));
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        await using (var ledgerStream = File.Create(ledgerJsonPath))
        {
            await JsonSerializer.SerializeAsync(ledgerStream, events, JsonOptions, cancellationToken);
        }

        var exceptionsPath = Path.Combine(outputDirectory ?? ".", "exceptions.csv");
        await File.WriteAllTextAsync(exceptionsPath, BuildExceptionsCsv(events), cancellationToken);
    }

    private static string BuildExceptionsCsv(IEnumerable<LedgerEvent> events)
    {
        var builder = new StringBuilder();
        builder.AppendLine("eventId,sourceSystem,sourceFile,sourceRowNumber,rawData");

        foreach (var ledgerEvent in events.Where(e => e.EventType == LedgerEventType.Unknown))
        {
            builder
                .Append(EscapeCsv(ledgerEvent.Id.ToString()))
                .Append(',')
                .Append(EscapeCsv(ledgerEvent.SourceReference.SourceSystem))
                .Append(',')
                .Append(EscapeCsv(ledgerEvent.SourceReference.SourceFile))
                .Append(',')
                .Append(ledgerEvent.SourceReference.SourceRowNumber.ToString(CultureInfo.InvariantCulture))
                .Append(',')
                .Append(EscapeCsv(ledgerEvent.SourceReference.RawData))
                .AppendLine();
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
