using LedgerForge.Tax.Italy.Rw;

namespace LedgerForge.Tests;

public sealed class TaxDossierPdfGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_WritesProfessionalReviewPdf()
    {
        var root = Directory.CreateTempSubdirectory("ledgerforge-tax-dossier-");
        try
        {
            var ledgerPath = Path.Combine(root.FullName, "ledger.json");
            var handoffPath = Path.Combine(root.FullName, "accountant-handoff-2025.json");
            var rwPath = Path.Combine(root.FullName, "italy-rw-accountant-2025.json");
            var outputFolder = Path.Combine(root.FullName, "accountant");

            await File.WriteAllTextAsync(ledgerPath, """{"events":[]}""");
            await File.WriteAllTextAsync(handoffPath, FakeHandoffJson);
            await File.WriteAllTextAsync(rwPath, FakeRwJson);

            var result = await new TaxDossierPdfGenerator().GenerateAsync(new TaxDossierPdfRequest(
                2025,
                ledgerPath,
                handoffPath,
                rwPath,
                outputFolder,
                null,
                "abc123",
                "0.0.0-test"));

            var pdfPath = Path.Combine(outputFolder, "LedgerForge-Tax-Dossier-2025.pdf");

            Assert.Equal("LedgerForge-Tax-Dossier-2025.pdf", result.GeneratedFileName);
            Assert.Equal("NOT READY FOR FILING", result.ReadinessStatus);
            Assert.True(File.Exists(pdfPath));
            Assert.True(new FileInfo(pdfPath).Length > 0);
            Assert.Equal(1, result.SourceFileCount);
            Assert.Equal(2, result.ImportedRowCount);
            Assert.Equal(2, result.LedgerEventCount);
            Assert.Equal(0, result.UnknownEventCount);
            Assert.Equal(2, result.MissingValuationEvidenceCount);
            Assert.Equal(1, result.ValidationErrorCount);
            Assert.Equal(1, result.WarningCount);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    private const string FakeHandoffJson =
        """
        {
          "year": 2025,
          "readinessStatus": "NOT READY FOR AUTONOMOUS FILING",
          "counts": {
            "sourceFileCount": 1,
            "importedRowCount": 2,
            "ledgerEventCount": 2,
            "unknownEventCount": 0,
            "officialReportDocumentCount": 1,
            "assetsDetectedCount": 1,
            "filledValuationEvidenceCount": 0,
            "missingValuationEvidenceCount": 2,
            "assetsMissingValuationEvidenceCount": 1
          },
          "sourceFilesSummary": [
            {
              "sourceSystem": "Fake",
              "sourceFile": "fake.csv",
              "importedRowCount": 2,
              "eventCount": 2,
              "unknownEventCount": 0
            }
          ],
          "reconciliationStatus": {
            "status": "AvailableForReview",
            "officialReportsAvailable": true,
            "reportTypes": [
              "ItalyAnnualBalanceReport"
            ]
          },
          "accountantChecklist": [
            { "item": "Confirm ownership title" },
            { "item": "Confirm ownership percentage" }
          ]
        }
        """;

    private const string FakeRwJson =
        """
        {
          "report": {
            "validationMessages": [
              {
                "severity": "Error",
                "code": "MissingValuation",
                "message": "Fake missing valuation."
              },
              {
                "severity": "Warning",
                "code": "AmbiguousForeignState",
                "message": "Fake warning."
              }
            ]
          }
        }
        """;
}
