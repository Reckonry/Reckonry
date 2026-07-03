namespace Reckonry.Reconciliation.Binance.Italy;

public interface IBinanceReportReader
{
    IReadOnlyList<BinanceReportDocument> ReadFolder(string inputFolder);

    BinanceReportDocument ReadFile(string pdfPath);
}
