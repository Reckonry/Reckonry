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
echo

run_reckonry import binance \
  --input "$REPO_ROOT/samples/demo/binance" \
  --out "$DEMO_ROOT/ledger.json"

run_reckonry validate \
  --input "$DEMO_ROOT/ledger.json"

run_reckonry audit \
  --input "$DEMO_ROOT/ledger.json" \
  --out "$DEMO_ROOT/audit"

run_reckonry report rw-snapshot \
  --input "$DEMO_ROOT/ledger.json" \
  --year "$YEAR" \
  --out "$DEMO_ROOT/reports"

run_reckonry report rw-value \
  --input "$DEMO_ROOT/ledger.json" \
  --year "$YEAR" \
  --out "$DEMO_ROOT/reports"

run_reckonry reconcile binance \
  --reports "$REPO_ROOT/samples/demo/official-reports" \
  --ledger-reports "$DEMO_ROOT/reports" \
  --out "$DEMO_ROOT/reconciliation"

run_reckonry config italy-rw-template \
  --year "$YEAR" \
  --ledger "$DEMO_ROOT/ledger.json" \
  --out "$DEMO_ROOT/config/italy-rw-$YEAR.template.json"

run_reckonry config italy-rw-fill-binance \
  --config "$DEMO_ROOT/config/italy-rw-$YEAR.template.json" \
  --reconciliation "$DEMO_ROOT/reconciliation/reconciliation-summary.json" \
  --out "$DEMO_ROOT/config/italy-rw-$YEAR.binance-filled.json"

run_reckonry report italy-rw-accountant \
  --input "$DEMO_ROOT/ledger.json" \
  --year "$YEAR" \
  --out "$DEMO_ROOT/accountant" \
  --language en-US

cp "$REPO_ROOT/samples/demo/italy-rw/accountant-handoff-$YEAR.fake.json" \
  "$DEMO_ROOT/accountant/accountant-handoff-$YEAR.json"

run_reckonry report tax-dossier \
  --year "$YEAR" \
  --ledger "$DEMO_ROOT/ledger.json" \
  --handoff "$DEMO_ROOT/accountant/accountant-handoff-$YEAR.json" \
  --rw "$DEMO_ROOT/accountant/italy-rw-accountant-$YEAR.json" \
  --out "$DEMO_ROOT/accountant" \
  --language en-US

echo
echo "Demo complete. Generated outputs:"
find "$DEMO_ROOT" -type f | sort | sed "s#^$REPO_ROOT/##"
