namespace Reckonry.Storage;

public sealed record LedgerValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;

    public static LedgerValidationResult Pass { get; } = new(Array.Empty<string>());
}
