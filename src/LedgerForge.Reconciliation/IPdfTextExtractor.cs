namespace LedgerForge.Reconciliation;

public interface IPdfTextExtractor
{
    ExtractedPdfText Extract(string pdfPath);
}
