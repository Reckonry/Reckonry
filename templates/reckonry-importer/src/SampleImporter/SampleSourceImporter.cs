using System.Globalization;
using Reckonry.Core;
using Reckonry.Importers.Abstractions;

namespace SampleImporter;

public sealed class ExampleSourceImporter : ISourceImporter
{
    public ImporterDescriptor Descriptor { get; } = new()
    {
        Id = "sample-importer",
        DisplayName = "Sample Source Importer",
        Provider = "Sample Provider",
        SourceKind = SourceKind.Exchange,
        ImporterVersion = "0.1.0-alpha",
        CoveragePercent = 10m,
        SupportedFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".csv" },
        SupportedFiles = ["Synthetic sample transaction CSV"],
        SupportedSchemas = ["timestamp,type,asset,amount,notes"],
        SupportedOperations = ["Deposits", "Withdrawals", "Unknown row preservation"]
    };

    public IReadOnlyList<LedgerEvent> ImportFolder(string inputFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFolder);

        if (!Directory.Exists(inputFolder))
        {
            throw new DirectoryNotFoundException($"Input folder was not found: {inputFolder}");
        }

        var events = new List<LedgerEvent>();
        foreach (var file in Directory.EnumerateFiles(inputFolder, "*.csv").Order())
        {
            events.AddRange(ImportFile(file));
        }

        return events;
    }

    private static IEnumerable<LedgerEvent> ImportFile(string file)
    {
        var rowNumber = 0;
        foreach (var rawRow in File.ReadLines(file))
        {
            rowNumber++;
            if (rowNumber == 1 || string.IsNullOrWhiteSpace(rawRow))
            {
                continue;
            }

            yield return ParseRow(file, rowNumber, rawRow);
        }
    }

    private static LedgerEvent ParseRow(string file, int rowNumber, string rawRow)
    {
        var columns = rawRow.Split(',');
        if (columns.Length < 4
            || !DateTimeOffset.TryParse(columns[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timestamp)
            || !decimal.TryParse(columns[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return Unknown(file, rowNumber, rawRow);
        }

        var type = columns[1].Trim().ToLowerInvariant();
        var asset = columns[2].Trim().ToUpperInvariant();

        return type switch
        {
            "deposit" => SinglePosting(file, rowNumber, rawRow, timestamp, LedgerEventType.Deposit, $"Sample deposit {asset}", asset, amount, LedgerPostingDirection.In),
            "withdrawal" => SinglePosting(file, rowNumber, rawRow, timestamp, LedgerEventType.Withdrawal, $"Sample withdrawal {asset}", asset, amount, LedgerPostingDirection.Out),
            _ => Unknown(file, rowNumber, rawRow, timestamp)
        };
    }

    private static LedgerEvent SinglePosting(
        string file,
        int rowNumber,
        string rawRow,
        DateTimeOffset timestamp,
        LedgerEventType eventType,
        string description,
        string asset,
        decimal amount,
        LedgerPostingDirection direction)
    {
        return new LedgerEvent(
            Guid.NewGuid(),
            timestamp,
            eventType,
            description,
            Source(file, rowNumber, rawRow),
            [new LedgerPosting(asset, Math.Abs(amount), direction, "Sample:Main")]);
    }

    private static LedgerEvent Unknown(
        string file,
        int rowNumber,
        string rawRow,
        DateTimeOffset? timestamp = null)
    {
        return new LedgerEvent(
            Guid.NewGuid(),
            timestamp ?? DateTimeOffset.UnixEpoch,
            LedgerEventType.Unknown,
            "Unsupported sample row preserved for later classification.",
            Source(file, rowNumber, rawRow),
            []);
    }

    private static SourceReference Source(string file, int rowNumber, string rawRow)
    {
        return new SourceReference("Sample Provider", Path.GetFileName(file), rowNumber, rawRow);
    }
}
