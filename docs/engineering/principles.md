# Reckonry Engineering Principles

These principles guide how Reckonry is designed, implemented, reviewed, and maintained.

## Correctness Over Performance

Reckonry handles financial records. Correct reconstruction, source preservation, and deterministic behavior matter more than speed. Performance optimizations are welcome only when they preserve correctness and remain covered by tests.

## Explainability Over Magic

Reckonry must make behavior understandable. Importer mappings, report numbers, audit findings, and reconciliation results should be traceable to clear rules and source data.

## Auditability Over Convenience

Convenient shortcuts must not reduce auditability. If a row is ambiguous, the system should preserve it and report uncertainty instead of hiding it behind a guess.

## Preserve Every Source

Every imported row or payload must remain traceable through `SourceReference`. Source system, source file, row number, and raw data are part of the evidence trail.

## Never Invent Financial Data

Reckonry must not fabricate quantities, values, fees, prices, timestamps, or classifications. Missing financial data remains missing until a documented, explicit, and testable source provides it.

## Never Silently Ignore Unknown Data

Unsupported or unrecognized data must become explicit unknown data. Unknown rows must be preserved and surfaced through reports, audit checks, or exceptions.

## Every Report Must Be Reproducible

Reports should be generated from the canonical ledger and deterministic inputs. A user must be able to rerun the same command against the same ledger and understand where each number came from.

## Ledger Is Immutable

The canonical ledger is the source of truth. Reports, audit checks, reconciliation, pricing, and tax modules may consume it, but must not mutate existing ledger events.

## Parsing and Tax Logic Must Remain Separated

Importers reconstruct factual events from source exports. Tax modules interpret the ledger. Importers must not embed jurisdiction-specific tax rules, and tax modules must not rewrite imported events.

## Every Calculation Must Be Testable

Any calculation that affects a report, audit finding, reconciliation result, or validation result must be testable with fake or anonymized data.

## Decimal Everywhere

Financial and crypto amounts must use `decimal`, never `double` or binary floating point. JSON numbers must round-trip safely into decimal values.

## UTC Internally

Ledger timestamps are normalized to UTC internally. Source-specific timestamps may be preserved in raw source data, but canonical events use UTC.

## Privacy By Default

Real exports, generated ledgers, reports, audit outputs, and reconciliation outputs are private by default. They belong in ignored local folders such as `input/` and `output/`, not in tests, samples, docs, logs, screenshots, or commits.
