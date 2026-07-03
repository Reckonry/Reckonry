namespace Reckonry.Reconciliation;

public interface IPdfTextExtractor
{
    ExtractedPdfText Extract(string pdfPath);
}
