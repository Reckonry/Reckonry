namespace Reckonry.Reconciliation;

public interface IBinanceReportReader
{
    IReadOnlyList<BinanceReportDocument> ReadFolder(string inputFolder);

    BinanceReportDocument ReadFile(string pdfPath);
}
