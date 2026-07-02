namespace LedgerForge.Reconciliation;

public sealed class DirectPdfTextExtractor : IPdfTextExtractor
{
    public ExtractedPdfText Extract(string pdfPath)
    {
        return PdfTextExtractor.Extract(pdfPath);
    }
}
