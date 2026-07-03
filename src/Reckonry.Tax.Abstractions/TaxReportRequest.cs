using Reckonry.Core;

namespace Reckonry.Tax.Abstractions;

public sealed record TaxReportRequest(
    int Year,
    IReadOnlyCollection<LedgerEvent> LedgerEvents);
