using Reckonry.Reconciliation.Binance.Italy;

namespace Reckonry.Tests;

public sealed class BinanceReportReaderTests
{
    [Fact]
    public void ReadFile_DetectsFakeTaxCertificationPdf()
    {
        using var fixture = FakePdfFixture.Create(
            "tax-certification.pdf",
            "Binance Italy Tax Certification Tax Year: 2025 Document Language: English Total Value EUR: 100.00");

        var reader = new BinanceReportReader();

        var document = reader.ReadFile(fixture.PdfPath);

        Assert.Equal(BinanceReportType.ItalyTaxCertification, document.Metadata.ReportType);
        Assert.Equal(2025, document.Metadata.TaxYear);
        Assert.Equal("en", document.Metadata.DocumentLanguage);
        Assert.Equal(1, document.Metadata.PageCount);
        Assert.True(document.Metadata.ExtractionSucceeded);
        Assert.False(document.Metadata.IsImageOnly);
        Assert.NotEmpty(document.Fields);
    }

    [Fact]
    public void ReadFile_DetectsFakeAnnualBalancePdf()
    {
        using var fixture = FakePdfFixture.Create(
            "annual-balance.pdf",
            "Binance Italy Annual Balance Report Year: 2024 Asset Rows: 2 Total EUR: 100.00");

        var reader = new BinanceReportReader();

        var document = reader.ReadFile(fixture.PdfPath);

        Assert.Equal(BinanceReportType.ItalyAnnualBalanceReport, document.Metadata.ReportType);
        Assert.Equal(2024, document.Metadata.TaxYear);
        Assert.True(document.Metadata.ExtractionSucceeded);
        Assert.NotEmpty(document.Fields);
    }

    [Fact]
    public void ReadFile_DetectsImageOnlyPdfAsOcrRequired()
    {
        using var fixture = FakePdfFixture.CreateImageOnly("image-only.pdf");

        var reader = new BinanceReportReader();

        var document = reader.ReadFile(fixture.PdfPath);

        Assert.True(document.Metadata.IsImageOnly);
        Assert.False(document.Metadata.ExtractionSucceeded);
    }

    private sealed class FakePdfFixture : IDisposable
    {
        private FakePdfFixture(DirectoryInfo folder, string pdfPath)
        {
            Folder = folder;
            PdfPath = pdfPath;
        }

        public DirectoryInfo Folder { get; }

        public string PdfPath { get; }

        public static FakePdfFixture Create(string fileName, string text)
        {
            var folder = Directory.CreateTempSubdirectory("reckonry-fake-pdf-");
            var path = Path.Combine(folder.FullName, fileName);
            File.WriteAllText(path, BuildTextPdf(text));
            return new FakePdfFixture(folder, path);
        }

        public static FakePdfFixture CreateImageOnly(string fileName)
        {
            var folder = Directory.CreateTempSubdirectory("reckonry-fake-pdf-");
            var path = Path.Combine(folder.FullName, fileName);
            File.WriteAllText(path, BuildTextPdf(string.Empty));
            return new FakePdfFixture(folder, path);
        }

        public void Dispose()
        {
            Folder.Delete(recursive: true);
        }

        private static string BuildTextPdf(string text)
        {
            var escaped = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            var streamText = string.IsNullOrEmpty(text)
                ? "q 10 10 100 100 re Q"
                : $"BT /F1 12 Tf 72 720 Td ({escaped}) Tj ET";

            return $"""
                   %PDF-1.4
                   1 0 obj
                   << /Type /Catalog /Pages 2 0 R >>
                   endobj
                   2 0 obj
                   << /Type /Pages /Kids [3 0 R] /Count 1 >>
                   endobj
                   3 0 obj
                   << /Type /Page /Parent 2 0 R /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>
                   endobj
                   4 0 obj
                   << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>
                   endobj
                   5 0 obj
                   << /Length {streamText.Length} >>
                   stream
                   {streamText}
                   endstream
                   endobj
                   trailer
                   << /Root 1 0 R >>
                   %%EOF
                   """;
        }
    }
}
