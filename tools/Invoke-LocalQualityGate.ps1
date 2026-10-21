[CmdletBinding()]
param(
    [switch]$SkipRestore,
    [switch]$ListSteps
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$frontendRoot = Join-Path $repositoryRoot 'ClientApp'
$evidenceRoot = Join-Path $repositoryRoot 'artifacts\local-quality-gate'
$runId = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$evidencePath = Join-Path $evidenceRoot "$runId.json"
$migrationPath = Join-Path $evidenceRoot "$runId-migrations.sql"
$results = [Collections.Generic.List[object]]::new()
$startedAt = [DateTime]::UtcNow

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory)] [string]$FilePath,
        [Parameter(Mandatory)] [string[]]$ArgumentList,
        [Parameter(Mandatory)] [string]$WorkingDirectory
    )

    Push-Location $WorkingDirectory
    try {
        & $FilePath @ArgumentList
        if ($LASTEXITCODE -ne 0) {
            throw "Command '$FilePath' failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

function Invoke-QualityStep {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [scriptblock]$Action
    )

    Write-Host "`n==> $Name" -ForegroundColor Cyan
    $stepStartedAt = [DateTime]::UtcNow
    try {
        & $Action
        $results.Add([pscustomobject]@{
            name = $Name
            outcome = 'passed'
            durationSeconds = [Math]::Round(([DateTime]::UtcNow - $stepStartedAt).TotalSeconds, 2)
        })
    }
    catch {
        $results.Add([pscustomobject]@{
            name = $Name
            outcome = 'failed'
            durationSeconds = [Math]::Round(([DateTime]::UtcNow - $stepStartedAt).TotalSeconds, 2)
            error = $_.Exception.Message
        })
        throw
    }
}

function Invoke-NativeCommandWithTimeout {
    param(
        [Parameter(Mandatory)] [string]$FilePath,
        [Parameter(Mandatory)] [string[]]$ArgumentList,
        [Parameter(Mandatory)] [string]$WorkingDirectory,
        [ValidateRange(1, 3600)] [int]$TimeoutSeconds = 300
    )

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FilePath
    $startInfo.WorkingDirectory = $WorkingDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($argument in $ArgumentList) {
        $startInfo.ArgumentList.Add($argument)
    }

    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    if (-not $process.Start()) {
        throw "Command '$FilePath' could not be started."
    }

    $standardOutput = $process.StandardOutput.ReadToEndAsync()
    $standardError = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        $process.Kill($true)
        $process.WaitForExit()
        throw "Command '$FilePath' exceeded the local quality-gate timeout of $TimeoutSeconds seconds."
    }

    $output = @($standardOutput.GetAwaiter().GetResult(), $standardError.GetAwaiter().GetResult()) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    if ($process.ExitCode -ne 0) {
        throw "Command '$FilePath' failed with exit code $($process.ExitCode)."
    }
    return ($output -join "`n")
}

function Invoke-BackendDependencyAudit {
    $audit = Invoke-NativeCommandWithTimeout 'dotnet' @(
        'list', 'FTCERP.Host.csproj', 'package', '--vulnerable', '--include-transitive', '--no-restore'
    ) $repositoryRoot 300
    $audit | Write-Output
    if ($audit -match 'has the following vulnerable packages') {
        throw 'The backend dependency audit found one or more vulnerable packages.'
    }
}

$stepNames = @(
    'Validate hosted/local workflow parity'
    if (-not $SkipRestore) { 'Restore backend, tests, and local tools' }
    'Build backend'
    'Test backend'
    'Validate migrations'
    'Validate version-controlled OpenAPI contract'
    'Validate recovery and performance safety contracts'
    'Audit backend dependencies'
    if (-not $SkipRestore) { 'Install frontend dependencies' }
    'Type-check frontend'
    'Lint frontend'
    'Test frontend'
    'Test accessibility baseline'
    'Build frontend and enforce bundle budget'
    'Audit frontend dependencies'
    'Self-test and scan pushable content for credential-shaped values'
)

if ($ListSteps) {
    $stepNames | ForEach-Object { Write-Output $_ }
    exit 0
}

if ($env:OPMS_LOCAL_QUALITY_GATE_ACTIVE -eq '1') {
    throw 'A nested OPMS local quality-gate invocation was blocked.'
}
$env:OPMS_LOCAL_QUALITY_GATE_ACTIVE = '1'

[IO.Directory]::CreateDirectory($evidenceRoot) | Out-Null
$succeeded = $false
$failure = $null

Push-Location $repositoryRoot
try {
    foreach ($requiredCommand in @('dotnet', 'node', 'npm', 'git', 'pwsh')) {
        if (-not (Get-Command $requiredCommand -ErrorAction SilentlyContinue)) {
            throw "Required local command '$requiredCommand' is not installed or is not on PATH."
        }
    }

    Invoke-QualityStep 'Validate hosted/local workflow parity' {
        & (Join-Path $PSScriptRoot 'Test-LocalQualityGateParity.ps1') -RepositoryRoot $repositoryRoot
    }

    if (-not $SkipRestore) {
        Invoke-QualityStep 'Restore backend, tests, and local tools' {
            Invoke-NativeCommand 'dotnet' @('restore', 'tests/FTCERP.Tests/FTCERP.Tests.csproj', '--configfile', 'NuGet.Config') $repositoryRoot
            Invoke-NativeCommand 'dotnet' @('restore', 'tools/FTCERP.OpenApiExporter/FTCERP.OpenApiExporter.csproj', '--configfile', 'NuGet.Config') $repositoryRoot
        }
    }

    Invoke-QualityStep 'Build backend' {
        Invoke-NativeCommand 'dotnet' @('build', 'FTCERP.Host.csproj', '--no-restore', '-p:SkipClientBuild=true', '--configuration', 'Release') $repositoryRoot
    }

    Invoke-QualityStep 'Test backend' {
        Invoke-NativeCommand 'dotnet' @('test', 'tests/FTCERP.Tests/FTCERP.Tests.csproj', '--no-restore', '-p:SkipClientBuild=true', '--configuration', 'Release') $repositoryRoot
    }

    Invoke-QualityStep 'Validate migrations' {
        Invoke-NativeCommand 'dotnet' @('ef', '--version') $repositoryRoot
        Invoke-NativeCommand 'dotnet' @('ef', 'migrations', 'script', '--project', 'FTCERP.Host.csproj', '--no-build', '--configuration', 'Release', '--idempotent', '--output', $migrationPath) $repositoryRoot
        if (-not (Test-Path -LiteralPath $migrationPath) -or (Get-Item -LiteralPath $migrationPath).Length -eq 0) {
            throw 'The idempotent migration script was not generated or was empty.'
        }
    }

    Invoke-QualityStep 'Validate version-controlled OpenAPI contract' {
        Invoke-NativeCommand 'dotnet' @(
            'run', '--project', 'tools/FTCERP.OpenApiExporter/FTCERP.OpenApiExporter.csproj', '--no-restore',
            '--configuration', 'Release', '--', '--output', 'docs/api/openapi-v1.json', '--check'
        ) $repositoryRoot
    }

    Invoke-QualityStep 'Validate recovery and performance safety contracts' {
        & (Join-Path $PSScriptRoot 'Invoke-OpmsSqlServerRecoveryDrill.ps1') -ValidateOnly
        Invoke-NativeCommand 'dotnet' @('run', '--project', 'tools/FTCERP.RecoveryDrill/FTCERP.RecoveryDrill.csproj', '--no-restore', '--configuration', 'Release', '--', '--validate-only') $repositoryRoot
        Invoke-NativeCommand 'dotnet' @('run', '--project', 'tools/FTCERP.PerformanceAcceptance/FTCERP.PerformanceAcceptance.csproj', '--no-restore', '--configuration', 'Release', '--', '--validate-only') $repositoryRoot
        Invoke-NativeCommand 'dotnet' @('run', '--project', 'tools/FTCERP.DataCutoverAcceptance/FTCERP.DataCutoverAcceptance.csproj', '--no-restore', '--configuration', 'Release', '--', '--validate-only') $repositoryRoot
    }

    Invoke-QualityStep 'Audit backend dependencies' { Invoke-BackendDependencyAudit }

    if (-not $SkipRestore) {
        Invoke-QualityStep 'Install frontend dependencies' {
            Invoke-NativeCommand 'npm' @('ci') $frontendRoot
        }
    }

    Invoke-QualityStep 'Type-check frontend' { Invoke-NativeCommand 'npm' @('run', 'typecheck') $frontendRoot }
    Invoke-QualityStep 'Lint frontend' { Invoke-NativeCommand 'npm' @('run', 'lint') $frontendRoot }
    Invoke-QualityStep 'Test frontend' { Invoke-NativeCommand 'npm' @('run', 'test:run') $frontendRoot }
    Invoke-QualityStep 'Test accessibility baseline' { Invoke-NativeCommand 'npm' @('run', 'test:a11y') $frontendRoot }
    Invoke-QualityStep 'Build frontend and enforce bundle budget' { Invoke-NativeCommand 'npm' @('run', 'build:verify') $frontendRoot }
    Invoke-QualityStep 'Audit frontend dependencies' { Invoke-NativeCommand 'npm' @('audit', '--audit-level=high') $frontendRoot }
    Invoke-QualityStep 'Self-test and scan pushable content for credential-shaped values' {
        & (Join-Path $PSScriptRoot 'Test-RepositorySecrets.ps1') -SelfTest
        & (Join-Path $PSScriptRoot 'Test-RepositorySecrets.ps1') -RepositoryRoot $repositoryRoot
    }

    $succeeded = $true
}
catch {
    $failure = $_.Exception.Message
    throw
}
finally {
    $finishedAt = [DateTime]::UtcNow
    $commit = (& git -C $repositoryRoot rev-parse HEAD 2>$null)
    $branch = (& git -C $repositoryRoot branch --show-current 2>$null)
    $trackedChangesPresent = -not [string]::IsNullOrWhiteSpace((& git -C $repositoryRoot status --short --untracked-files=no 2>$null | Out-String))
    $evidence = [ordered]@{
        schemaVersion = 1
        gate = 'OPMS local workflow-parity quality gate'
        succeeded = $succeeded
        startedAtUtc = $startedAt.ToString('O')
        finishedAtUtc = $finishedAt.ToString('O')
        durationSeconds = [Math]::Round(($finishedAt - $startedAt).TotalSeconds, 2)
        branch = ([string]$branch).Trim()
        commit = ([string]$commit).Trim()
        trackedChangesPresent = $trackedChangesPresent
        skipRestore = [bool]$SkipRestore
        localOnly = $true
        cloudResourcesCreated = $false
        failure = $failure
        steps = @($results)
    }
    $evidence | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $evidencePath -Encoding utf8
    Pop-Location
    Write-Host "`nLocal quality-gate evidence: $evidencePath" -ForegroundColor DarkGray
}

Write-Host "`nLocal quality gate passed: $($results.Count) steps in $([Math]::Round(([DateTime]::UtcNow - $startedAt).TotalSeconds, 2)) seconds." -ForegroundColor Green
