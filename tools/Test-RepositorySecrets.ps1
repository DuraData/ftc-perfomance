[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '..'),
    [string]$BaseRef,
    [switch]$SelfTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$secretRules = @(
    [pscustomobject]@{ Name = 'Private key'; Pattern = 'BEGIN (RSA |EC |OPENSSH |DSA )?PRIVATE KEY' },
    [pscustomobject]@{ Name = 'AWS access key'; Pattern = 'AKIA[0-9A-Z]{16}' },
    [pscustomobject]@{ Name = 'GitHub token'; Pattern = 'gh[pousr]_[A-Za-z0-9_]{30,}' },
    [pscustomobject]@{ Name = 'Slack token'; Pattern = 'xox[baprs]-[A-Za-z0-9-]{20,}' },
    [pscustomobject]@{ Name = 'Stripe live secret'; Pattern = 'sk_live_[A-Za-z0-9]{20,}' },
    [pscustomobject]@{ Name = 'SendGrid API key'; Pattern = 'SG\.[A-Za-z0-9_-]{16,}\.[A-Za-z0-9_-]{20,}' },
    [pscustomobject]@{ Name = 'Google API key'; Pattern = 'AIza[0-9A-Za-z_-]{30,}' },
    [pscustomobject]@{ Name = 'npm access token'; Pattern = 'npm_[A-Za-z0-9]{30,}' },
    [pscustomobject]@{ Name = 'Azure Storage account key'; Pattern = 'AccountKey=[A-Za-z0-9+/]{40,}={0,2}' }
)

function Invoke-Git {
    param(
        [Parameter(Mandatory)] [string]$Root,
        [Parameter(Mandatory)] [string[]]$Arguments,
        [switch]$AllowNoMatches
    )

    $output = @(& git -C $Root @Arguments 2>$null)
    $exitCode = $LASTEXITCODE
    if ($AllowNoMatches -and $exitCode -eq 1) {
        return @()
    }
    if ($exitCode -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $exitCode."
    }
    return $output
}

function Resolve-OutgoingBaseRef {
    param(
        [Parameter(Mandatory)] [string]$Root,
        [string]$RequestedBaseRef
    )

    if (-not [string]::IsNullOrWhiteSpace($RequestedBaseRef)) {
        Invoke-Git $Root @('rev-parse', '--verify', "$RequestedBaseRef^{commit}") | Out-Null
        return $RequestedBaseRef
    }

    $branch = (Invoke-Git $Root @('branch', '--show-current') | Select-Object -First 1).Trim()
    if ([string]::IsNullOrWhiteSpace($branch)) {
        return $null
    }

    $upstream = @(Invoke-Git $Root @('for-each-ref', '--format=%(upstream:short)', "refs/heads/$branch")) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -First 1
    if (-not [string]::IsNullOrWhiteSpace($upstream)) {
        return $upstream.Trim()
    }

    foreach ($candidate in @("origin/$branch", 'origin/main')) {
        & git -C $Root rev-parse --verify "$candidate^{commit}" 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            return $candidate
        }
    }

    return $null
}

function Find-RepositorySecretFindings {
    param(
        [Parameter(Mandatory)] [string]$Root,
        [string]$RequestedBaseRef
    )

    $resolvedRoot = [IO.Path]::GetFullPath($Root)
    Invoke-Git $resolvedRoot @('rev-parse', '--is-inside-work-tree') | Out-Null
    $findings = [Collections.Generic.List[object]]::new()

    foreach ($rule in $secretRules) {
        $workingMatches = @(Invoke-Git $resolvedRoot @('grep', '-I', '-l', '-E', $rule.Pattern, '--', '.') -AllowNoMatches)
        foreach ($path in $workingMatches) {
            if (-not [string]::IsNullOrWhiteSpace($path)) {
                $findings.Add([pscustomobject]@{
                    Rule = $rule.Name
                    Location = 'tracked working tree'
                    Commit = $null
                    Path = $path.Trim()
                })
            }
        }
    }

    $base = Resolve-OutgoingBaseRef $resolvedRoot $RequestedBaseRef
    if (-not [string]::IsNullOrWhiteSpace($base)) {
        $commits = @(Invoke-Git $resolvedRoot @('rev-list', '--reverse', "$base..HEAD"))
        foreach ($commit in $commits) {
            if ([string]::IsNullOrWhiteSpace($commit)) {
                continue
            }
            foreach ($rule in $secretRules) {
                $commitMatches = @(Invoke-Git $resolvedRoot @('grep', '-I', '-l', '-E', $rule.Pattern, $commit, '--', '.') -AllowNoMatches)
                foreach ($match in $commitMatches) {
                    if ([string]::IsNullOrWhiteSpace($match)) {
                        continue
                    }
                    $path = $match.Trim()
                    $prefix = "$commit`:"
                    if ($path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
                        $path = $path.Substring($prefix.Length)
                    }
                    $findings.Add([pscustomobject]@{
                        Rule = $rule.Name
                        Location = 'outgoing commit'
                        Commit = $commit
                        Path = $path
                    })
                }
            }
        }
    }

    return @($findings | Sort-Object Rule, Location, Commit, Path -Unique)
}

function Assert-NoRepositorySecrets {
    param(
        [Parameter(Mandatory)] [string]$Root,
        [string]$RequestedBaseRef
    )

    $findings = @(Find-RepositorySecretFindings $Root $RequestedBaseRef)
    if ($findings.Count -eq 0) {
        Write-Host 'No high-confidence credential shapes were found in tracked content or outgoing commits.'
        return
    }

    $safeSummary = $findings | ForEach-Object {
        $commitLabel = if ($_.Commit) { $_.Commit.Substring(0, 12) } else { 'worktree' }
        "[$($_.Rule)] $commitLabel $($_.Path)"
    }
    throw "The repository secret scan found $($findings.Count) credential-shaped location(s). Values were intentionally not printed:`n$($safeSummary -join "`n")"
}

function Invoke-SecretScannerSelfTest {
    $temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ("opms-secret-scan-{0}" -f [Guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
    try {
        Invoke-Git $temporaryRoot @('init', '--quiet') | Out-Null
        Invoke-Git $temporaryRoot @('config', 'user.email', 'scanner-test@opms.local') | Out-Null
        Invoke-Git $temporaryRoot @('config', 'user.name', 'OPMS Secret Scanner') | Out-Null

        $fixturePath = Join-Path $temporaryRoot 'fixture.txt'
        [IO.File]::WriteAllText($fixturePath, 'safe fixture')
        Invoke-Git $temporaryRoot @('add', 'fixture.txt') | Out-Null
        Invoke-Git $temporaryRoot @('commit', '--quiet', '-m', 'safe baseline') | Out-Null
        $baseline = (Invoke-Git $temporaryRoot @('rev-parse', 'HEAD') | Select-Object -First 1).Trim()

        if (@(Find-RepositorySecretFindings $temporaryRoot $baseline).Count -ne 0) {
            throw 'The safe self-test fixture produced a secret finding.'
        }

        $syntheticToken = 'AKIA' + ('A' * 16)
        [IO.File]::WriteAllText($fixturePath, "temporary synthetic token $syntheticToken")
        if (@(Find-RepositorySecretFindings $temporaryRoot $baseline).Count -eq 0) {
            throw 'The scanner did not detect a credential shape in the tracked working tree.'
        }

        Invoke-Git $temporaryRoot @('add', 'fixture.txt') | Out-Null
        Invoke-Git $temporaryRoot @('commit', '--quiet', '-m', 'temporary secret') | Out-Null
        [IO.File]::WriteAllText($fixturePath, 'safe again')
        Invoke-Git $temporaryRoot @('add', 'fixture.txt') | Out-Null
        Invoke-Git $temporaryRoot @('commit', '--quiet', '-m', 'remove temporary secret') | Out-Null

        $historyFindings = @(Find-RepositorySecretFindings $temporaryRoot $baseline)
        if (-not ($historyFindings | Where-Object { $_.Location -eq 'outgoing commit' })) {
            throw 'The scanner did not detect a credential shape that was committed and later removed.'
        }

        Write-Host 'Repository secret scanner self-test passed.'
    }
    finally {
        $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
        $resolvedSystemTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
        if ($resolvedTemporaryRoot.StartsWith($resolvedSystemTemp, [StringComparison]::OrdinalIgnoreCase) -and
            (Split-Path $resolvedTemporaryRoot -Leaf).StartsWith('opms-secret-scan-', [StringComparison]::Ordinal)) {
            Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
        }
    }
}

if ($SelfTest) {
    Invoke-SecretScannerSelfTest
}
else {
    Assert-NoRepositorySecrets ([IO.Path]::GetFullPath($RepositoryRoot)) $BaseRef
}
