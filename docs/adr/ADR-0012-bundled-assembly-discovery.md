# ADR-0012: Bundled Assembly Discovery

## Status

Accepted

## Context

The CLI and API need to discover installed Reckonry importers, reports, tax
modules, reconciliation modules, and pricing providers without maintaining
large hardcoded descriptor lists.

Reckonry does not yet have a stable external plugin loading, sandboxing, or
package compatibility model.

## Decision

Hosts use bundled assembly discovery through `PluginScanner.ScanPlugins()`.
The scanner finds non-abstract implementations of known Reckonry interfaces in
loaded `Reckonry.*` assemblies and assemblies present in the host output
directory.

This model must be documented as bundled assembly discovery, not as mature
external plugin loading.

## Consequences

Host projects still reference concrete modules so their assemblies are present
at runtime.

The current model is suitable for public alpha descriptor discovery, but it is
not a security boundary and not a stable third-party plugin runtime.

Future external plugin loading requires a separate ADR.
