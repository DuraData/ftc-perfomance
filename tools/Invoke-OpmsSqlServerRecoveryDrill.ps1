[CmdletBinding()]
param(
    [switch]$ValidateOnly,
    [switch]$ConfirmLocalDestructiveRecoveryDrill,
    [string]$ConnectionStringEnvironmentVariable = 'OPMS_RECOVERY_CONNECTION_STRING',
    [string]$BackupDirectory = '',
    [ValidateRange(1, 1440)]
    [int]$RecoveryPointObjectiveMinutes = 15,
    [ValidateRange(1, 1440)]
    [int]$RecoveryTimeObjectiveMinutes = 60,
    [switch]$KeepRestoredDatabase
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertTo-SqlLiteral([string]$Value) {
    return $Value.Replace("'", "''")
}

function ConvertTo-SqlIdentifier([string]$Value) {
    return $Value.Replace(']', ']]')
}

function Invoke-OpmsSqlcmd {
    param(
        [Parameter(Mandatory)] [string]$Server,
        [Parameter(Mandatory)] [string]$Database,
        [Parameter(Mandatory)] [string]$User,
        [Parameter(Mandatory)] [string]$Password,
        [Parameter(Mandatory)] [string]$Query,
        [switch]$Delimited
    )

    $previousPassword = $env:SQLCMDPASSWORD
    try {
        $env:SQLCMDPASSWORD = $Password
        $arguments = @('-b', '-S', $Server, '-d', $Database, '-U', $User, '-Q', $Query)
        if ($Delimited) { $arguments += @('-h', '-1', '-W', '-s', '|') }
        $output = & sqlcmd @arguments 2>&1
        if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed with exit code $LASTEXITCODE. $($output -join [Environment]::NewLine)" }
        return $output
    }
    finally {
        $env:SQLCMDPASSWORD = $previousPassword
    }
}

if ($ValidateOnly) {
    [pscustomobject]@{
        Script = 'OPMS SQL Server recovery drill'
        SafetyGate = 'ConfirmLocalDestructiveRecoveryDrill'
        AllowedServers = @('localhost\SQLEXPRESS', '.\SQLEXPRESS', '(local)\SQLEXPRESS')
        AllowedDatabases = @('OPMS_Development', 'OPMS_IntegrationTests')
        Operations = @('BACKUP DATABASE COPY_ONLY CHECKSUM', 'RESTORE VERIFYONLY CHECKSUM', 'isolated RESTORE DATABASE', 'DBCC CHECKDB', 'isolated database cleanup')
    } | ConvertTo-Json -Depth 3
    exit 0
}

if (-not $ConfirmLocalDestructiveRecoveryDrill) {
    throw 'Pass -ConfirmLocalDestructiveRecoveryDrill to acknowledge creation and cleanup of an isolated local restore database.'
}

$connectionString = [Environment]::GetEnvironmentVariable($ConnectionStringEnvironmentVariable)
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    throw "Environment variable '$ConnectionStringEnvironmentVariable' must contain the local SQL Server recovery-drill connection string."
}

$builder = [System.Data.Common.DbConnectionStringBuilder]::new()
$builder.ConnectionString = $connectionString
function Get-ConnectionValue([string[]]$Keys) {
    foreach ($key in $Keys) {
        if ($builder.ContainsKey($key)) { return [string]$builder[$key] }
    }
    return ''
}

$server = Get-ConnectionValue @('Server', 'Data Source')
$database = Get-ConnectionValue @('Database', 'Initial Catalog')
$user = Get-ConnectionValue @('User Id', 'UID')
$password = Get-ConnectionValue @('Password', 'PWD')
$allowedServers = @('localhost\SQLEXPRESS', '.\SQLEXPRESS', '(local)\SQLEXPRESS')
$allowedDatabases = @('OPMS_Development', 'OPMS_IntegrationTests')

if ($server -notin $allowedServers) { throw "Refusing recovery drill against non-local server '$server'." }
if ($database -notin $allowedDatabases) { throw "Refusing recovery drill against database '$database'. Allowed local test databases: $($allowedDatabases -join ', ')." }
if ([string]::IsNullOrWhiteSpace($user) -or [string]::IsNullOrWhiteSpace($password)) { throw 'The recovery drill requires a SQL login supplied only through the configured environment variable.' }
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { throw 'sqlcmd is required for the SQL Server recovery drill.' }

if ([string]::IsNullOrWhiteSpace($BackupDirectory)) {
    $BackupDirectory = Join-Path $PSScriptRoot '..\artifacts\recovery-drills'
}
$resolvedParent = [IO.Path]::GetFullPath($BackupDirectory)
[IO.Directory]::CreateDirectory($resolvedParent) | Out-Null

$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$suffix = [Guid]::NewGuid().ToString('N').Substring(0, 8)
$restoreDatabase = "${database}_RestoreDrill_$suffix"
$backupPath = Join-Path $resolvedParent "${database}_$stamp.bak"
$dataPath = Join-Path $resolvedParent "${restoreDatabase}.mdf"
$logPath = Join-Path $resolvedParent "${restoreDatabase}_log.ldf"
$evidencePath = Join-Path $resolvedParent "${database}_$stamp.json"
$startedAt = [DateTime]::UtcNow
$restoreCreated = $false
$succeeded = $false
$backupVerified = $false
$restoreIntegrityVerified = $false
$failure = $null

try {
    $backupLiteral = ConvertTo-SqlLiteral $backupPath
    $sourceIdentifier = ConvertTo-SqlIdentifier $database
    Invoke-OpmsSqlcmd -Server $server -Database master -User $user -Password $password -Query "BACKUP DATABASE [$sourceIdentifier] TO DISK = N'$backupLiteral' WITH COPY_ONLY, INIT, CHECKSUM, COMPRESSION, STATS = 10;" | Out-Null
    Invoke-OpmsSqlcmd -Server $server -Database master -User $user -Password $password -Query "RESTORE VERIFYONLY FROM DISK = N'$backupLiteral' WITH CHECKSUM;" | Out-Null
    $backupVerified = $true

    $fileRows = Invoke-OpmsSqlcmd -Server $server -Database master -User $user -Password $password -Delimited -Query "RESTORE FILELISTONLY FROM DISK = N'$backupLiteral';"
    $parsedRows = @($fileRows | ForEach-Object {
        $parts = ([string]$_).Split('|')
        if ($parts.Length -ge 3 -and $parts[2].Trim() -in @('D', 'L')) {
            [pscustomobject]@{ LogicalName = $parts[0].Trim(); Type = $parts[2].Trim() }
        }
    })
    $dataLogicalName = ($parsedRows | Where-Object Type -eq 'D' | Select-Object -First 1).LogicalName
    $logLogicalName = ($parsedRows | Where-Object Type -eq 'L' | Select-Object -First 1).LogicalName
    if ([string]::IsNullOrWhiteSpace($dataLogicalName) -or [string]::IsNullOrWhiteSpace($logLogicalName)) {
        throw 'The backup file list did not contain both a data file and a log file.'
    }

    $restoreIdentifier = ConvertTo-SqlIdentifier $restoreDatabase
    $dataLogicalLiteral = ConvertTo-SqlLiteral $dataLogicalName
    $logLogicalLiteral = ConvertTo-SqlLiteral $logLogicalName
    $dataLiteral = ConvertTo-SqlLiteral $dataPath
    $logLiteral = ConvertTo-SqlLiteral $logPath
    $restoreQuery = "RESTORE DATABASE [$restoreIdentifier] FROM DISK = N'$backupLiteral' WITH MOVE N'$dataLogicalLiteral' TO N'$dataLiteral', MOVE N'$logLogicalLiteral' TO N'$logLiteral', RECOVERY, CHECKSUM, STATS = 10;"
    Invoke-OpmsSqlcmd -Server $server -Database master -User $user -Password $password -Query $restoreQuery | Out-Null
    $restoreCreated = $true
    Invoke-OpmsSqlcmd -Server $server -Database $restoreDatabase -User $user -Password $password -Query "DBCC CHECKDB (N'$restoreIdentifier') WITH NO_INFOMSGS, ALL_ERRORMSGS;" | Out-Null
    $restoreIntegrityVerified = $true

    $elapsed = [DateTime]::UtcNow - $startedAt
    if ($elapsed.TotalMinutes -gt $RecoveryTimeObjectiveMinutes) {
        throw "Recovery drill exceeded the configured RTO of $RecoveryTimeObjectiveMinutes minutes."
    }
    if (([DateTime]::UtcNow - (Get-Item -LiteralPath $backupPath).LastWriteTimeUtc).TotalMinutes -gt $RecoveryPointObjectiveMinutes) {
        throw "The verified backup exceeded the configured RPO age of $RecoveryPointObjectiveMinutes minutes."
    }
    $succeeded = $true
}
catch {
    $failure = $_.Exception.Message
    throw
}
finally {
    $cleanupFailure = $null
    if ($restoreCreated -and -not $KeepRestoredDatabase) {
        try {
            $restoreIdentifier = ConvertTo-SqlIdentifier $restoreDatabase
            Invoke-OpmsSqlcmd -Server $server -Database master -User $user -Password $password -Query "ALTER DATABASE [$restoreIdentifier] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$restoreIdentifier];" | Out-Null
            $restoreCreated = $false
        }
        catch {
            $cleanupFailure = $_.Exception.Message
            $failure = @($failure, "Isolated restore cleanup failed: $cleanupFailure") | Where-Object { $_ } | Join-String -Separator ' '
        }
    }

    $completedAt = [DateTime]::UtcNow
    [pscustomobject]@{
        SchemaVersion = 1
        SourceServer = $server
        SourceDatabase = $database
        RestoredDatabase = $restoreDatabase
        BackupPath = $backupPath
        StartedAtUtc = $startedAt.ToString('O')
        CompletedAtUtc = $completedAt.ToString('O')
        DurationSeconds = [Math]::Round(($completedAt - $startedAt).TotalSeconds, 2)
        RecoveryPointObjectiveMinutes = $RecoveryPointObjectiveMinutes
        RecoveryTimeObjectiveMinutes = $RecoveryTimeObjectiveMinutes
        BackupVerified = $backupVerified
        RestoreIntegrityVerified = $restoreIntegrityVerified
        IsolatedRestoreStillExists = [bool]$restoreCreated
        RetainedByRequest = [bool]($restoreCreated -and $KeepRestoredDatabase)
        CleanupFailed = [bool]$cleanupFailure
        Failure = $failure
    } | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $evidencePath -Encoding utf8
    Write-Output "Recovery drill evidence: $evidencePath"
    if ($cleanupFailure -and $succeeded) { throw "Recovery verification passed, but isolated restore cleanup failed: $cleanupFailure" }
}
