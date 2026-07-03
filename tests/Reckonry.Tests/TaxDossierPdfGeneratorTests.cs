using Reckonry.Tax.Italy.Rw;

namespace Reckonry.Tests;

public sealed class TaxDossierPdfGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_WritesProfessionalReviewPdf()
    {
        var root = Directory.CreateTempSubdirectory("reckonry-tax-dossier-");
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

            var pdfPath = Path.Combine(outputFolder, "Reckonry-Tax-Dossier-2025.pdf");

            Assert.Equal("Reckonry-Tax-Dossier-2025.pdf", result.GeneratedFileName);
            Assert.Equal(ReportLanguages.Italian, result.Language);
            Assert.Equal("Dossier fiscale cripto", result.Title);
            Assert.Equal("NON PRONTO PER LA DICHIARAZIONE", result.ReadinessStatus);
            Assert.Equal(0, result.PortfolioAssetCount);
            Assert.Equal(0, result.MovementTimelineActiveMonthCount);
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

    [Fact]
    public async Task GenerateAsync_WithValuationEvidenceAndLedgerEvents_RendersChartPaths()
    {
        var root = Directory.CreateTempSubdirectory("reckonry-tax-dossier-charts-");
        try
        {
            var ledgerPath = Path.Combine(root.FullName, "ledger.json");
            var handoffPath = Path.Combine(root.FullName, "accountant-handoff-2025.json");
            var rwPath = Path.Combine(root.FullName, "italy-rw-accountant-2025.json");
            var outputFolder = Path.Combine(root.FullName, "accountant");

            await File.WriteAllTextAsync(ledgerPath, FakeLedgerWithEventsJson);
            await File.WriteAllTextAsync(handoffPath, FakeHandoffJson);
            await File.WriteAllTextAsync(rwPath, FakeRwWithValuationJson);

            var result = await new TaxDossierPdfGenerator().GenerateAsync(new TaxDossierPdfRequest(
                2025,
                ledgerPath,
                handoffPath,
                rwPath,
                outputFolder,
                null,
                "abc123",
                "0.0.0-test",
                RepositoryUrl: "https://example.com/reckonry",
                Language: ReportLanguages.English));

            Assert.Equal(1, result.PortfolioAssetCount);
            Assert.Equal(2, result.MovementTimelineActiveMonthCount);
            Assert.True(new FileInfo(Path.Combine(outputFolder, result.GeneratedFileName)).Length > 0);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task GenerateAsync_WithExplicitEnglishLanguage_ReturnsEnglishLabels()
    {
        var root = Directory.CreateTempSubdirectory("reckonry-tax-dossier-en-");
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
                "0.0.0-test",
                Language: ReportLanguages.English));

            Assert.Equal(ReportLanguages.English, result.Language);
            Assert.Equal("Crypto Tax Dossier", result.Title);
            Assert.Equal("NOT READY FOR FILING", result.ReadinessStatus);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task GenerateAsync_WithUnknownLanguage_FailsClearly()
    {
        var root = Directory.CreateTempSubdirectory("reckonry-tax-dossier-language-");
        try
        {
            var ledgerPath = Path.Combine(root.FullName, "ledger.json");
            var handoffPath = Path.Combine(root.FullName, "accountant-handoff-2025.json");
            var rwPath = Path.Combine(root.FullName, "italy-rw-accountant-2025.json");
            var outputFolder = Path.Combine(root.FullName, "accountant");

            await File.WriteAllTextAsync(ledgerPath, """{"events":[]}""");
            await File.WriteAllTextAsync(handoffPath, FakeHandoffJson);
            await File.WriteAllTextAsync(rwPath, FakeRwJson);

            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                new TaxDossierPdfGenerator().GenerateAsync(new TaxDossierPdfRequest(
                    2025,
                    ledgerPath,
                    handoffPath,
                    rwPath,
                    outputFolder,
                    null,
                    "abc123",
                    "0.0.0-test",
                    Language: "fr-FR")));

            Assert.Contains("Supported languages: it-IT, en-US", exception.Message);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void Localizer_DoesNotTranslateLegalCodes()
    {
        var localizer = DictionaryTextLocalizer.Create(ReportLanguages.Italian);

        Assert.Contains("RW", localizer.Text("Section.RwDraft"));
        Assert.Contains("RW8", localizer.Text("Section.Rw8Draft"));
        Assert.Equal("IC", localizer.Text("Legal.IC"));
        Assert.Equal("IVAFE", localizer.Text("Legal.IVAFE"));
        Assert.Equal("IVIE", localizer.Text("Legal.IVIE"));
    }

    [Fact]
    public void ResolveStatusKind_MapsBadgeRenderingStates()
    {
        Assert.Equal(TaxDossierPdfGenerator.DossierStatusKind.Pass, TaxDossierPdfGenerator.ResolveStatusKind(0, 0));
        Assert.Equal(TaxDossierPdfGenerator.DossierStatusKind.Warning, TaxDossierPdfGenerator.ResolveStatusKind(0, 1));
        Assert.Equal(TaxDossierPdfGenerator.DossierStatusKind.Error, TaxDossierPdfGenerator.ResolveStatusKind(1, 0));
        Assert.Equal(TaxDossierPdfGenerator.DossierStatusKind.NotApplicable, TaxDossierPdfGenerator.ResolveStatusKind(0, 0, applies: false));
    }

    [Fact]
    public void BuildVerificationQrPayload_DoesNotIncludePrivateFinancialValues()
    {
        var payload = TaxDossierPdfGenerator.BuildVerificationQrPayload(
            "https://example.com/reckonry",
            "abc123",
            "0.0.0-test",
            "def456",
            new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero));

        Assert.Contains("repository=https://example.com/reckonry", payload);
        Assert.Contains("ledger_sha256=abc123", payload);
        Assert.Contains("reckonry_version=0.0.0-test", payload);
        Assert.Contains("git_commit=def456", payload);
        Assert.Contains("generated_utc=2025-01-02T03:04:05.0000000+00:00", payload);
        Assert.DoesNotContain("BTC", payload);
        Assert.DoesNotContain("EUR", payload);
        Assert.DoesNotContain("amount", payload, StringComparison.OrdinalIgnoreCase);
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

    private const string FakeLedgerWithEventsJson =
        """
        [
          { "timestampUtc": "2025-01-10T00:00:00+00:00" },
          { "timestampUtc": "2025-01-11T00:00:00+00:00" },
          { "timestampUtc": "2025-03-01T00:00:00+00:00" },
          { "timestampUtc": "2024-03-01T00:00:00+00:00" }
        ]
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

    private const string FakeRwWithValuationJson =
        """
        {
          "report": {
            "cryptoLines": [
              {
                "assetSymbol": "BTC",
                "column8FinalValue": 1000.00,
                "finalValueEvidence": {
                  "sourceName": "Fake official report"
                }
              },
              {
                "assetSymbol": "ETH",
                "column8FinalValue": null,
                "finalValueEvidence": null
              }
            ],
            "validationMessages": [
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
