[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$gitDirectory = Join-Path $repositoryRoot '.git'
$hooksDirectory = Join-Path $repositoryRoot '.githooks'
$prePushHook = Join-Path $hooksDirectory 'pre-push'

if (-not (Test-Path -LiteralPath $gitDirectory)) {
    throw "'$repositoryRoot' is not a Git working tree."
}

if (-not (Test-Path -LiteralPath $prePushHook)) {
    throw "The repository-owned pre-push hook is missing at '$prePushHook'."
}

& git -C $repositoryRoot config --local core.hooksPath .githooks
if ($LASTEXITCODE -ne 0) {
    throw 'Git could not configure the repository-owned hooks directory.'
}

$configuredHooksPath = (& git -C $repositoryRoot config --local --get core.hooksPath).Trim()
if ($LASTEXITCODE -ne 0 -or $configuredHooksPath -ne '.githooks') {
    throw "Git hook verification failed. Configured path: '$configuredHooksPath'."
}

Write-Output 'Installed the repository-owned pre-push quality gate for this checkout.'
Write-Output 'Every git push now runs tools/Invoke-LocalQualityGate.ps1 and fails closed.'
