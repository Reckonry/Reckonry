namespace LedgerForge.Tax.Italy.Rw;

public interface ITextLocalizer
{
    string Language { get; }

    string Text(string key);

    string Format(string key, params object[] args);
}
