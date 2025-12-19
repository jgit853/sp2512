param(
    [switch]$RestoreOnly
)

$solutionPath = Join-Path $PSScriptRoot "src/ComplianceAssistant.sln"
if (-not (Test-Path $solutionPath)) {
    Write-Error "Solution file not found at $solutionPath"
    exit 1
}

if ($RestoreOnly) {
    dotnet restore $solutionPath
    exit $LASTEXITCODE
}

dotnet restore $solutionPath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet build $solutionPath -c Release
