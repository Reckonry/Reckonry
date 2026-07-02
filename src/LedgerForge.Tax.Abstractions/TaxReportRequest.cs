using LedgerForge.Core;

namespace LedgerForge.Tax.Abstractions;

public sealed record TaxReportRequest(
    int Year,
    IReadOnlyCollection<LedgerEvent> LedgerEvents);
