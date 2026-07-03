namespace Reckonry.Tax.Italy.Rw;

public sealed record ItalyRwReport(
    int Year,
    IReadOnlyList<ItalyRwLine> CryptoLines,
    ItalyRw8Summary Rw8,
    IReadOnlyList<RwValidationMessage> ValidationMessages)
{
    public bool CanFinalize => ValidationMessages.All(message => message.Severity != RwValidationSeverity.Error);

    public IReadOnlyList<RwValidationMessage> Warnings =>
        ValidationMessages.Where(message => message.Severity == RwValidationSeverity.Warning).ToArray();

    public IReadOnlyList<RwValidationMessage> Errors =>
        ValidationMessages.Where(message => message.Severity == RwValidationSeverity.Error).ToArray();
}
