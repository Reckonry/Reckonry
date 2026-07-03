namespace LedgerForge.Tax.Italy.Rw;

public sealed record TaxDossierPdfRequest(
    int Year,
    string LedgerJsonPath,
    string AccountantHandoffJsonPath,
    string AccountantRwJsonPath,
    string OutputFolder,
    string? LogoSvgPath,
    string? GitCommit,
    string LedgerForgeVersion,
    string? RepositoryUrl = null,
    string? Language = null);
