using System.Collections.Concurrent;
using Reckonry.Audit;
using Reckonry.Core;
using Reckonry.Importers.Abstractions;
using Reckonry.Plugins;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(_ => PluginScanner.ScanPlugins());
builder.Services.AddSingleton(sp => new ImporterRegistry(sp.GetRequiredService<PluginCatalog>().Importers));
builder.Services.AddSingleton<IImporterFactory, ImporterFactory>();
builder.Services.AddSingleton<IIntegrityChecker, IntegrityChecker>();
builder.Services.AddSingleton<InMemoryLedgerRepository>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "Reckonry.Api",
    status = "Experimental architecture preview",
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
      <title>Reckonry Experimental API Metadata</title>
    </head>
    <body>
      <h1>Reckonry Experimental API Metadata</h1>
      <p>Preview metadata: <a href="/swagger/v1/swagger.json">/swagger/v1/swagger.json</a></p>
      <p>This is a hand-authored OpenAPI-shaped preview document for an experimental in-memory host.</p>
      <p>The API has no authentication, no database, no persistence, and no production hardening.</p>
    </body>
    </html>
    """,
    "text/html"));

app.MapGet("/swagger/v1/swagger.json", () => Results.Json(OpenApiDocument.Build()));

app.MapGet("/importers", (IImporterFactory importerFactory) =>
{
    return Results.Ok(importerFactory.ListImporters());
});

app.MapGet("/plugins", (PluginCatalog plugins, IImporterFactory importerFactory) =>
{
    return Results.Ok(new PluginResponse(
        importerFactory.ListImporters(),
        plugins.TaxModules.Select(module => module.Descriptor).OrderBy(descriptor => descriptor.CountryCode).ToArray(),
        plugins.Reports.Select(report => report.Descriptor).OrderBy(descriptor => descriptor.Id).ToArray(),
        plugins.ReconciliationModules.Select(module => module.Descriptor).OrderBy(descriptor => descriptor.Id).ToArray(),
        plugins.PricingProviders.Select(provider => provider.ProviderId).Order(StringComparer.OrdinalIgnoreCase).ToArray()));
});

app.MapPost("/import", (ImportRequest request, IImporterFactory importerFactory, InMemoryLedgerRepository repository) =>
{
    var source = request.ResolveSource();
    if (string.IsNullOrWhiteSpace(source))
    {
        return Results.BadRequest(new ErrorResponse(["source is required."]));
    }

    if (string.IsNullOrWhiteSpace(request.InputFolder))
    {
        return Results.BadRequest(new ErrorResponse(["inputFolder is required."]));
    }

    if (!importerFactory.TryCreate(source, out var importer))
    {
        return Results.NotFound(new ErrorResponse([$"No importer is registered for source '{source}'."]));
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

app.MapGet("/reports", (PluginCatalog plugins) =>
{
    return Results.Ok(plugins.Reports.Select(report => report.Descriptor).OrderBy(descriptor => descriptor.Id));
});

app.MapPost("/reports", (ReportRequest request, InMemoryLedgerRepository repository, PluginCatalog plugins) =>
{
    if (!repository.TryGet(request.LedgerId, out var events))
    {
        return Results.NotFound(new ErrorResponse([$"Ledger '{request.LedgerId}' was not found in memory."]));
    }

    if (request.Year is < 1 or > 9999)
    {
        return Results.BadRequest(new ErrorResponse(["year must be between 1 and 9999."]));
    }

    var descriptor = plugins.Reports
        .Select(report => report.Descriptor)
        .FirstOrDefault(report => string.Equals(report.Id, request.ReportType, StringComparison.OrdinalIgnoreCase));

    if (descriptor is null)
    {
        return Results.BadRequest(new ErrorResponse(["reportType is not installed. Use GET /reports for descriptors."]));
    }

    if (descriptor.Scope != Reckonry.Reports.ReportScope.Generic)
    {
        return Results.BadRequest(new ErrorResponse([$"Report '{descriptor.Id}' is scoped to country/provider plugins and is not exposed as a generic report."]));
    }

    return Results.Ok(new ReportResponse(
        request.LedgerId,
        descriptor.Id,
        request.Year,
        descriptor,
        events.Count));
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

internal sealed record ImportRequest(string? Source, string InputFolder, string? Exchange = null)
{
    public string? ResolveSource()
    {
        return string.IsNullOrWhiteSpace(Source) ? Exchange : Source;
    }
}

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
    object Descriptor,
    int EventCount);

internal sealed record PluginResponse(
    IReadOnlyList<ImporterDescriptor> Importers,
    IReadOnlyList<Reckonry.Tax.Abstractions.TaxModuleDescriptor> Countries,
    IReadOnlyList<Reckonry.Reports.ReportDescriptor> Reports,
    IReadOnlyList<Reckonry.Reconciliation.Abstractions.ReconciliationModuleDescriptor> ReconciliationModules,
    IReadOnlyList<string> PricingProviders);

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
                title = "Reckonry.Api Experimental Host",
                version = "0.1.0",
                description = "Experimental architecture preview for in-memory Reckonry workflows. Not a stable public API contract."
            },
            paths = new Dictionary<string, object>
            {
                ["/import"] = new
                {
                    post = new
                    {
                        summary = "Import source data into an in-memory ledger.",
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
                        summary = "Generate generic report metadata from an in-memory ledger.",
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
                        summary = "List registered source importers.",
                        responses = Responses("ImporterDescriptor[]")
                    }
                },
                ["/plugins"] = new
                {
                    get = new
                    {
                        summary = "List installed platform modules.",
                        responses = Responses("PluginResponse")
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
