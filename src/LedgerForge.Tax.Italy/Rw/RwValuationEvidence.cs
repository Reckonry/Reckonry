namespace LedgerForge.Tax.Italy.Rw;

public abstract record RwValuationEvidence(
    decimal ValueEur,
    string SourceName,
    DateTimeOffset SourceTimestamp,
    decimal Confidence,
    string Notes);

public sealed record ExchangeValue(
    decimal ValueEur,
    string SourceName,
    DateTimeOffset SourceTimestamp,
    decimal Confidence,
    string Notes)
    : RwValuationEvidence(ValueEur, SourceName, SourceTimestamp, Confidence, Notes);

public sealed record AnalogousPlatformValue(
    decimal ValueEur,
    string SourceName,
    DateTimeOffset SourceTimestamp,
    decimal Confidence,
    string Notes)
    : RwValuationEvidence(ValueEur, SourceName, SourceTimestamp, Confidence, Notes);

public sealed record MarketDataSiteValue(
    decimal ValueEur,
    string SourceName,
    DateTimeOffset SourceTimestamp,
    decimal Confidence,
    string Notes)
    : RwValuationEvidence(ValueEur, SourceName, SourceTimestamp, Confidence, Notes);

public sealed record AcquisitionCostFallback(
    decimal ValueEur,
    string SourceName,
    DateTimeOffset SourceTimestamp,
    decimal Confidence,
    string Notes)
    : RwValuationEvidence(ValueEur, SourceName, SourceTimestamp, Confidence, Notes);
