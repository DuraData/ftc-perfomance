[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '..'),
    [string[]]$LocalStepNames
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$resolvedRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$manifestPath = Join-Path $PSScriptRoot 'quality-gate-parity.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json

function Assert-SequenceEqual {
    param(
        [Parameter(Mandatory)] [string]$Label,
        [Parameter(Mandatory)] [string[]]$Expected,
        [Parameter(Mandatory)] [string[]]$Actual
    )

    $expectedJson = ConvertTo-Json @($Expected) -Compress
    $actualJson = ConvertTo-Json @($Actual) -Compress
    if ($expectedJson -ne $actualJson) {
        throw "$Label drifted from tools/quality-gate-parity.json.`nExpected: $expectedJson`nActual:   $actualJson"
    }
}

$workflowPath = Join-Path $resolvedRoot ([string]$manifest.workflow)
$workflowText = Get-Content -LiteralPath $workflowPath -Raw
$hostedSteps = @(
    [regex]::Matches($workflowText, '(?m)^\s*-\s+name:\s*(?<name>.+?)\s*$') |
        ForEach-Object { $_.Groups['name'].Value.Trim().Trim('''', '"') }
)
$expectedHostedSteps = @($manifest.mappings | ForEach-Object { [string]$_.hostedStep })
Assert-SequenceEqual 'Hosted workflow steps' $expectedHostedSteps $hostedSteps

if (-not $PSBoundParameters.ContainsKey('LocalStepNames')) {
    $localGatePath = Join-Path $resolvedRoot ([string]$manifest.localGate)
    $LocalStepNames = @(& pwsh -NoLogo -NoProfile -File $localGatePath -ListSteps)
    if ($LASTEXITCODE -ne 0) {
        throw "The local gate could not list its stages (exit code $LASTEXITCODE)."
    }
}

$expectedLocalSteps = @($manifest.localSteps | ForEach-Object { [string]$_ })
Assert-SequenceEqual 'Local quality-gate steps' $expectedLocalSteps @($LocalStepNames)

$unknownMappings = @(
    $manifest.mappings |
        Where-Object { $expectedLocalSteps -notcontains [string]$_.localStep }
)
if ($unknownMappings.Count -gt 0) {
    throw "One or more hosted checks map to an unknown local stage: $($unknownMappings.hostedStep -join ', ')."
}

foreach ($relativePath in @([string]$manifest.localGate, [string]$manifest.localHook)) {
    $localAutomationPath = Join-Path $resolvedRoot $relativePath
    $localAutomation = Get-Content -LiteralPath $localAutomationPath -Raw
    if ($localAutomation -match '(?im)^\s*(?:&\s*)?gh(?:\.exe)?\s') {
        throw "GitHub CLI invocation detected in local verification automation: $relativePath."
    }
}

Write-Output "Local workflow parity passed: $($hostedSteps.Count) hosted checks map to $($expectedLocalSteps.Count) local stages; GitHub CLI is not used."
