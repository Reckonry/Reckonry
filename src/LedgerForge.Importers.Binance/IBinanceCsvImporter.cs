using LedgerForge.Core;

namespace LedgerForge.Importers.Binance;

public interface IBinanceCsvImporter
{
    IReadOnlyList<LedgerEvent> ImportFolder(string inputFolder);
}
