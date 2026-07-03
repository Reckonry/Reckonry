$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "..")
Set-Location $RepoRoot

$DemoRoot = Join-Path $RepoRoot "artifacts/demo"
$CliDll = Join-Path $RepoRoot "src/Reckonry.Cli/bin/Debug/net10.0/Reckonry.Cli.dll"
$Year = "2025"

$ExpectedDemoRoot = Join-Path $RepoRoot "artifacts/demo"
if ($DemoRoot -ne $ExpectedDemoRoot) {
    throw "Refusing to clean unexpected demo output path: $DemoRoot"
}

if (Test-Path $DemoRoot) {
    Remove-Item -Recurse -Force $DemoRoot
}
New-Item -ItemType Directory -Force -Path $DemoRoot | Out-Null

if (-not (Test-Path $CliDll)) {
    throw "Built CLI was not found: $CliDll. Run dotnet build Reckonry.sln before ./scripts/demo.ps1."
}

function Invoke-Reckonry {
    dotnet $CliDll @args
    if ($LASTEXITCODE -ne 0) {
        throw "Reckonry command failed: $args"
    }
}

Write-Host "Reckonry public demo"
Write-Host "Input data is synthetic and safe to commit publicly."
Write-Host ""

Invoke-Reckonry import binance `
    --input (Join-Path $RepoRoot "samples/demo/binance") `
    --out (Join-Path $DemoRoot "ledger.json")

Invoke-Reckonry validate `
    --input (Join-Path $DemoRoot "ledger.json")

Invoke-Reckonry audit `
    --input (Join-Path $DemoRoot "ledger.json") `
    --out (Join-Path $DemoRoot "audit")

Invoke-Reckonry report rw-snapshot `
    --input (Join-Path $DemoRoot "ledger.json") `
    --year $Year `
    --out (Join-Path $DemoRoot "reports")

Invoke-Reckonry report rw-value `
    --input (Join-Path $DemoRoot "ledger.json") `
    --year $Year `
    --out (Join-Path $DemoRoot "reports")

Invoke-Reckonry reconcile binance `
    --reports (Join-Path $RepoRoot "samples/demo/official-reports") `
    --ledger-reports (Join-Path $DemoRoot "reports") `
    --out (Join-Path $DemoRoot "reconciliation")

Invoke-Reckonry config italy-rw-template `
    --year $Year `
    --ledger (Join-Path $DemoRoot "ledger.json") `
    --out (Join-Path $DemoRoot "config/italy-rw-$Year.template.json")

Invoke-Reckonry config italy-rw-fill-binance `
    --config (Join-Path $DemoRoot "config/italy-rw-$Year.template.json") `
    --reconciliation (Join-Path $DemoRoot "reconciliation/reconciliation-summary.json") `
    --out (Join-Path $DemoRoot "config/italy-rw-$Year.binance-filled.json")

Invoke-Reckonry report italy-rw-accountant `
    --input (Join-Path $DemoRoot "ledger.json") `
    --year $Year `
    --out (Join-Path $DemoRoot "accountant") `
    --language en-US

Copy-Item `
    -Path (Join-Path $RepoRoot "samples/demo/italy-rw/accountant-handoff-$Year.fake.json") `
    -Destination (Join-Path $DemoRoot "accountant/accountant-handoff-$Year.json") `
    -Force

Invoke-Reckonry report tax-dossier `
    --year $Year `
    --ledger (Join-Path $DemoRoot "ledger.json") `
    --handoff (Join-Path $DemoRoot "accountant/accountant-handoff-$Year.json") `
    --rw (Join-Path $DemoRoot "accountant/italy-rw-accountant-$Year.json") `
    --out (Join-Path $DemoRoot "accountant") `
    --language en-US

Write-Host ""
Write-Host "Demo complete. Generated outputs:"
Get-ChildItem -Path $DemoRoot -Recurse -File |
    Sort-Object FullName |
    ForEach-Object { Resolve-Path -Relative $_.FullName }
