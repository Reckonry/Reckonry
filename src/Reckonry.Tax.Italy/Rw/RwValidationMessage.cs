namespace Reckonry.Tax.Italy.Rw;

public sealed record RwValidationMessage(
    RwValidationSeverity Severity,
    string Code,
    string Message,
    string? AssetSymbol = null);
