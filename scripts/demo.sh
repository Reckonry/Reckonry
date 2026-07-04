#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

DEMO_ROOT="$REPO_ROOT/artifacts/demo"
CLI_DLL="$REPO_ROOT/src/Reckonry.Cli/bin/Debug/net10.0/Reckonry.Cli.dll"
YEAR="2025"

case "$DEMO_ROOT" in
  "$REPO_ROOT"/artifacts/demo) ;;
  *)
    echo "Refusing to clean unexpected demo output path: $DEMO_ROOT" >&2
    exit 1
    ;;
esac

rm -rf "$DEMO_ROOT"
mkdir -p "$DEMO_ROOT"
export RECKONRY_SUPPRESS_REPOSITORY_INPUT_WARNING=1

if [ ! -f "$CLI_DLL" ]; then
  echo "Built CLI was not found: $CLI_DLL" >&2
  echo "Run dotnet build Reckonry.sln before scripts/demo.sh." >&2
  exit 1
fi

run_reckonry() {
  dotnet "$CLI_DLL" "$@"
}

echo "Reckonry public demo"
echo "Input data is synthetic and safe to commit publicly."
echo "Repository-path privacy warnings are suppressed for this synthetic demo only."
echo "Expected alpha result: NOT READY FOR FILING. That means missing professional inputs are visible and not invented."
echo

run_reckonry plugins

run_reckonry import binance \
  --input "$REPO_ROOT/samples/demo/binance" \
  --out "$DEMO_ROOT/ledger.json"

run_reckonry validate \
  --input "$DEMO_ROOT/ledger.json"

run_reckonry report integrity \
  --input "$DEMO_ROOT/ledger.json" \
  --out "$DEMO_ROOT/audit"

echo
echo "Second importer platform demo: Coinbase synthetic export"
run_reckonry import coinbase \
  --input "$REPO_ROOT/samples/demo/coinbase" \
  --out "$DEMO_ROOT/coinbase/ledger.json"

run_reckonry validate \
  --input "$DEMO_ROOT/coinbase/ledger.json"

run_reckonry report integrity \
  --input "$DEMO_ROOT/coinbase/ledger.json" \
  --out "$DEMO_ROOT/coinbase/audit"

run_reckonry reconcile coinbase global \
  --reports "$REPO_ROOT/samples/demo/coinbase-official-reports" \
  --ledger-reports "$DEMO_ROOT/coinbase" \
  --out "$DEMO_ROOT/coinbase/reconciliation"

run_reckonry tax italy rw snapshot \
  --input "$DEMO_ROOT/ledger.json" \
  --year "$YEAR" \
  --out "$DEMO_ROOT/reports"

run_reckonry tax italy rw value \
  --input "$DEMO_ROOT/ledger.json" \
  --year "$YEAR" \
  --out "$DEMO_ROOT/reports"

run_reckonry reconcile binance italy \
  --reports "$REPO_ROOT/samples/demo/official-reports" \
  --ledger-reports "$DEMO_ROOT/reports" \
  --out "$DEMO_ROOT/reconciliation"

run_reckonry tax italy rw template \
  --year "$YEAR" \
  --ledger "$DEMO_ROOT/ledger.json" \
  --out "$DEMO_ROOT/config/italy-rw-$YEAR.template.json"

run_reckonry tax italy rw fill binance \
  --config "$DEMO_ROOT/config/italy-rw-$YEAR.template.json" \
  --reconciliation "$DEMO_ROOT/reconciliation/reconciliation-summary.json" \
  --out "$DEMO_ROOT/config/italy-rw-$YEAR.binance-filled.json"

run_reckonry tax italy accountant \
  --input "$DEMO_ROOT/ledger.json" \
  --year "$YEAR" \
  --out "$DEMO_ROOT/accountant" \
  --language en-US

cp "$REPO_ROOT/samples/demo/italy-rw/accountant-handoff-$YEAR.fake.json" \
  "$DEMO_ROOT/accountant/accountant-handoff-$YEAR.json"

run_reckonry tax italy dossier \
  --year "$YEAR" \
  --ledger "$DEMO_ROOT/ledger.json" \
  --handoff "$DEMO_ROOT/accountant/accountant-handoff-$YEAR.json" \
  --rw "$DEMO_ROOT/accountant/italy-rw-accountant-$YEAR.json" \
  --out "$DEMO_ROOT/accountant" \
  --language en-US

echo
echo "Demo complete. Generated outputs:"
find "$DEMO_ROOT" -type f | sort | sed "s#^$REPO_ROOT/##"
echo
echo "What to inspect first:"
echo "- artifacts/demo/ledger.json"
echo "- artifacts/demo/audit/integrity.md"
echo "- artifacts/demo/coinbase/ledger.json"
echo "- artifacts/demo/coinbase/audit/integrity.md"
echo "- artifacts/demo/coinbase/reconciliation/reconciliation-summary.md"
echo "- artifacts/demo/reconciliation/reconciliation-summary.md"
echo "- artifacts/demo/accountant/italy-rw-accountant-$YEAR.md"
echo "- artifacts/demo/accountant/Reckonry-Tax-Dossier-$YEAR.pdf"
