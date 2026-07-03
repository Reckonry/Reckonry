namespace Reckonry.Storage;

public interface ILedgerValidator
{
    Task<LedgerValidationResult> ValidateFileAsync(
        string ledgerJsonPath,
        CancellationToken cancellationToken = default);

    LedgerValidationResult ValidateJson(string json);
}
