namespace Reckonry.Reconciliation.Binance.Italy;

public interface IPdfTextExtractor
{
    ExtractedPdfText Extract(string pdfPath);
}
