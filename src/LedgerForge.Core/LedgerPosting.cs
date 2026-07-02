namespace LedgerForge.Core;

public sealed record LedgerPosting(
    string AssetSymbol,
    decimal Amount,
    LedgerPostingDirection Direction,
    string Account,
    MoneyAmount? Value = null);
