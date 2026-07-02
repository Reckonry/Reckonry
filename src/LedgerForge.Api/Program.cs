using System.Collections.Concurrent;
using LedgerForge.Audit;
using LedgerForge.Core;
using LedgerForge.Importers.Abstractions;
using LedgerForge.Importers.Bitstamp;
using LedgerForge.Importers.Binance;
using LedgerForge.Importers.Coinbase;
using LedgerForge.Importers.CryptoCom;
using LedgerForge.Importers.Kraken;
using LedgerForge.Importers.Revolut;
using LedgerForge.Reports;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IExchangeImporter, BinanceCsvImporter>();
builder.Services.AddSingleton<IExchangeImporter, CoinbaseImporter>();
builder.Services.AddSingleton<IExchangeImporter, KrakenImporter>();
builder.Services.AddSingleton<IExchangeImporter, RevolutImporter>();
builder.Services.AddSingleton<IExchangeImporter, CryptoComImporter>();
builder.Services.AddSingleton<IExchangeImporter, BitstampImporter>();
builder.Services.AddSingleton<ImporterRegistry>();
builder.Services.AddSingleton<IImporterFactory, ImporterFactory>();
builder.Services.AddSingleton<IIntegrityChecker, IntegrityChecker>();
builder.Services.AddSingleton<InMemoryLedgerRepository>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "LedgerForge.Api",
    status = "Architecture preview",
    authentication = "None",
    database = "None",
    storage = "In-memory only",
    swagger = "/swagger/v1/swagger.json"
}));

app.MapGet("/swagger", () => Results.Content(
    """
    <!doctype html>
    <html lang="en">
    <head>
      <meta charset="utf-8">
      <title>LedgerForge API Swagger</title>
    </head>
    <body>
      <h1>LedgerForge API Swagger</h1>
      <p>OpenAPI document: <a href="/swagger/v1/swagger.json">/swagger/v1/swagger.json</a></p>
      <p>This API host is an architecture preview with no authentication, no database, and in-memory ledgers only.</p>
    </body>
    </html>
    """,
    "text/html"));

app.MapGet("/swagger/v1/swagger.json", () => Results.Json(OpenApiDocument.Build()));

app.MapGet("/importers", (IImporterFactory importerFactory) =>
{
    return Results.Ok(importerFactory.ListImporters());
});

app.MapPost("/import", (ImportRequest request, IImporterFactory importerFactory, InMemoryLedgerRepository repository) =>
{
    if (string.IsNullOrWhiteSpace(request.Exchange))
    {
        return Results.BadRequest(new ErrorResponse(["exchange is required."]));
    }

    if (string.IsNullOrWhiteSpace(request.InputFolder))
    {
        return Results.BadRequest(new ErrorResponse(["inputFolder is required."]));
    }

    if (!importerFactory.TryCreate(request.Exchange, out var importer))
    {
        return Results.NotFound(new ErrorResponse([$"No importer is registered for exchange '{request.Exchange}'."]));
    }

    try
    {
        var events = importer.ImportFolder(request.InputFolder);
        var ledgerId = repository.Save(events);
        return Results.Ok(new ImportResponse(
            ledgerId,
            importer.Descriptor.Id,
            importer.Descriptor.DisplayName,
            events.Count,
            events.Count(e => e.EventType == LedgerEventType.Unknown)));
    }
    catch (Exception ex) when (ex is NotSupportedException or DirectoryNotFoundException)
    {
        return Results.BadRequest(new ErrorResponse([ex.Message]));
    }
});

app.MapPost("/audit", (AuditRequest request, InMemoryLedgerRepository repository, IIntegrityChecker checker) =>
{
    if (!repository.TryGet(request.LedgerId, out var events))
    {
        return Results.NotFound(new ErrorResponse([$"Ledger '{request.LedgerId}' was not found in memory."]));
    }

    var report = checker.Check(events);
    return Results.Ok(new AuditResponse(
        request.LedgerId,
        report.IntegrityScore,
        report.ConfidenceScore,
        report.Warnings,
        report.Recommendations,
        report.Findings));
});

app.MapPost("/reports", (ReportRequest request, InMemoryLedgerRepository repository) =>
{
    if (!repository.TryGet(request.LedgerId, out var events))
    {
        return Results.NotFound(new ErrorResponse([$"Ledger '{request.LedgerId}' was not found in memory."]));
    }

    if (request.Year is < 1 or > 9999)
    {
        return Results.BadRequest(new ErrorResponse(["year must be between 1 and 9999."]));
    }

    return request.ReportType.Trim().ToLowerInvariant() switch
    {
        "rw-snapshot" => Results.Ok(new ReportResponse(
            request.LedgerId,
            request.ReportType,
            request.Year,
            RwSnapshotReportWriter.BuildRows(request.Year, events))),
        "rw-value" => Results.Ok(new ReportResponse(
            request.LedgerId,
            request.ReportType,
            request.Year,
            RwValueReportWriter.BuildRows(request.Year, events))),
        _ => Results.BadRequest(new ErrorResponse(["reportType must be 'rw-snapshot' or 'rw-value'."]))
    };
});

app.MapPost("/reconcile", (ReconcileRequest request, InMemoryLedgerRepository repository) =>
{
    if (!repository.TryGet(request.LedgerId, out var events))
    {
        return Results.NotFound(new ErrorResponse([$"Ledger '{request.LedgerId}' was not found in memory."]));
    }

    return Results.Ok(new ReconcileResponse(
        request.LedgerId,
        request.Provider,
        "ArchitectureOnly",
        events.Count,
        "Reconciliation providers are registered as a future read-only API concern. This minimal in-memory host does not read official PDFs or write reconciliation files."));
});

app.Run();

internal sealed class InMemoryLedgerRepository
{
    private readonly ConcurrentDictionary<Guid, IReadOnlyList<LedgerEvent>> ledgers = new();

    public Guid Save(IReadOnlyList<LedgerEvent> events)
    {
        var ledgerId = Guid.NewGuid();
        ledgers[ledgerId] = events;
        return ledgerId;
    }

    public bool TryGet(Guid ledgerId, out IReadOnlyList<LedgerEvent> events)
    {
        return ledgers.TryGetValue(ledgerId, out events!);
    }
}

internal sealed record ImportRequest(string Exchange, string InputFolder);

internal sealed record ImportResponse(
    Guid LedgerId,
    string ImporterId,
    string ImporterName,
    int EventCount,
    int UnknownEventCount);

internal sealed record AuditRequest(Guid LedgerId);

internal sealed record AuditResponse(
    Guid LedgerId,
    int IntegrityScore,
    int ConfidenceScore,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Recommendations,
    IReadOnlyList<IntegrityFinding> Findings);

internal sealed record ReportRequest(Guid LedgerId, string ReportType, int Year);

internal sealed record ReportResponse(
    Guid LedgerId,
    string ReportType,
    int Year,
    object Rows);

internal sealed record ReconcileRequest(Guid LedgerId, string Provider = "binance");

internal sealed record ReconcileResponse(
    Guid LedgerId,
    string Provider,
    string Status,
    int EventCount,
    string Message);

internal sealed record ErrorResponse(IReadOnlyList<string> Errors);

internal static class OpenApiDocument
{
    public static object Build()
    {
        return new
        {
            openapi = "3.0.3",
            info = new
            {
                title = "LedgerForge.Api",
                version = "0.1.0",
                description = "Architecture preview API for in-memory LedgerForge workflows."
            },
            paths = new Dictionary<string, object>
            {
                ["/import"] = new
                {
                    post = new
                    {
                        summary = "Import exchange data into an in-memory ledger.",
                        requestBody = JsonBody("ImportRequest"),
                        responses = Responses("ImportResponse")
                    }
                },
                ["/audit"] = new
                {
                    post = new
                    {
                        summary = "Run ledger integrity audit checks against an in-memory ledger.",
                        requestBody = JsonBody("AuditRequest"),
                        responses = Responses("AuditResponse")
                    }
                },
                ["/reports"] = new
                {
                    post = new
                    {
                        summary = "Generate report rows from an in-memory ledger.",
                        requestBody = JsonBody("ReportRequest"),
                        responses = Responses("ReportResponse")
                    }
                },
                ["/reconcile"] = new
                {
                    post = new
                    {
                        summary = "Architecture placeholder for read-only reconciliation workflows.",
                        requestBody = JsonBody("ReconcileRequest"),
                        responses = Responses("ReconcileResponse")
                    }
                },
                ["/importers"] = new
                {
                    get = new
                    {
                        summary = "List registered importer plugins.",
                        responses = Responses("ImporterDescriptor[]")
                    }
                }
            }
        };
    }

    private static object JsonBody(string schemaName)
    {
        return new
        {
            required = true,
            content = new Dictionary<string, object>
            {
                ["application/json"] = new
                {
                    schema = new
                    {
                        type = "object",
                        title = schemaName
                    }
                }
            }
        };
    }

    private static object Responses(string schemaName)
    {
        return new Dictionary<string, object>
        {
            ["200"] = new
            {
                description = "Success",
                content = new Dictionary<string, object>
                {
                    ["application/json"] = new
                    {
                        schema = new
                        {
                            type = "object",
                            title = schemaName
                        }
                    }
                }
            },
            ["400"] = new
            {
                description = "Validation error"
            },
            ["404"] = new
            {
                description = "Requested resource was not found"
            }
        };
    }
}
