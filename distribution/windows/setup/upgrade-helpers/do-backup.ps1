# Backs up Readarr before an in-place upgrade.
# Tries the native Readarr API first (produces a user-restorable zip in Backups/).
# Falls back to file-copy into C:\ProgramData\Readarr.backups\<timestamp>\ if API unreachable.
# Emits JSON on stdout: {"method":"ApiZip|FileCopy","path":"<full path>"} — last line is the JSON.
# Returns exit 0 always unless no backup was possible at all.

$ErrorActionPreference = 'SilentlyContinue'
$ProgressPreference = 'SilentlyContinue'

$ReadarrRoot = 'C:\ProgramData\Readarr'
$BackupRoot = 'C:\ProgramData\Readarr.backups'
$ConfigFile = Join-Path $ReadarrRoot 'config.xml'
$ProgressFile = Join-Path $env:TEMP 'readarr-backup-progress.txt'

function Emit-Result([string]$method, [string]$path) {
    Write-Output (@{method = $method; path = $path} | ConvertTo-Json -Compress)
}

# Write a one-line status marker the Inno wizard can poll and display.
# Last writer wins — no append. Swallow errors so a file-lock race doesn't
# blow up the backup.
function Write-Stage([string]$line) {
    try { [System.IO.File]::WriteAllText($ProgressFile, $line) } catch { }
}

function Invoke-FileCopyBackup {
    Write-Stage 'Running file-copy backup...'
    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $target = Join-Path $BackupRoot $timestamp
    New-Item -ItemType Directory -Path $target -Force | Out-Null

    $itemsToBackup = @('bin', 'config.xml',
                       'readarr.db', 'readarr.db-journal', 'readarr.db-wal', 'readarr.db-shm',
                       'Backups')
    foreach ($item in $itemsToBackup) {
        $src = Join-Path $ReadarrRoot $item
        if (-not (Test-Path $src)) { continue }

        if ($item -eq 'bin') {
            # bin/ is many small files where per-file overhead dominates.
            # robocopy /MT:8 parallelizes the copy across 8 threads — faster
            # than Copy-Item. /MT:8 is robocopy's own recommended default,
            # safe across dual-core through many-core hosts.
            $fileCount = (Get-ChildItem $src -Recurse -File -ErrorAction SilentlyContinue | Measure-Object).Count
            $sizeMB = [math]::Round((Get-ChildItem $src -Recurse -File -ErrorAction SilentlyContinue | Measure-Object -Sum Length).Sum / 1MB)
            Write-Stage "Copying binaries ($fileCount files, ~$sizeMB MB)..."
            $destBin = Join-Path $target 'bin'
            robocopy $src $destBin /E /MT:8 /NFL /NDL /NC /NS /NP | Out-Null
        }
        elseif ($item -eq 'readarr.db') {
            $dbMB = [math]::Round((Get-Item $src).Length / 1MB)
            Write-Stage "Copying database ($dbMB MB)..."
            Copy-Item -Recurse -Force -Path $src -Destination $target -ErrorAction SilentlyContinue
        }
        elseif ($item -eq 'Backups') {
            Write-Stage 'Copying existing Readarr backups...'
            Copy-Item -Recurse -Force -Path $src -Destination $target -ErrorAction SilentlyContinue
        }
        else {
            # Small single files (config.xml, db journals) — too quick to warrant a stage marker
            Copy-Item -Recurse -Force -Path $src -Destination $target -ErrorAction SilentlyContinue
        }
    }

    Write-Stage 'Cleaning installer artifacts from backup...'
    $targetBin = Join-Path $target 'bin'
    Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $targetBin 'Rollback-To-Pre-Upgrade.bat')
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue (Join-Path $targetBin 'upgrade-helpers')

    Write-Stage 'Rotating old backups...'
    Get-ChildItem $BackupRoot -Directory -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -Skip 2 |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

    Write-Stage 'Finalizing backup...'
    Emit-Result 'FileCopy' $target
}

# Try API-based backup
try {
    Write-Stage 'Checking Readarr API...'
    if (-not (Test-Path $ConfigFile)) { throw 'config.xml missing' }
    $cfg = [xml](Get-Content $ConfigFile -Raw)
    $port = $cfg.Config.Port
    $apiKey = $cfg.Config.ApiKey
    if (-not $port -or -not $apiKey) { throw 'port/apikey missing' }

    $headers = @{'X-Api-Key' = $apiKey}
    $body = @{name = 'Backup'} | ConvertTo-Json

    Write-Stage 'Creating backup zip via Readarr API...'
    $cmd = Invoke-RestMethod -Method POST `
                              -Uri "http://localhost:$port/api/v1/command" `
                              -Headers $headers -Body $body `
                              -ContentType 'application/json' -TimeoutSec 10
    $cmdId = $cmd.id

    # Poll up to 5 min
    $deadline = (Get-Date).AddMinutes(5)
    do {
        Start-Sleep -Seconds 2
        Write-Stage 'Waiting for Readarr to finish building the backup...'
        $status = Invoke-RestMethod -Uri "http://localhost:$port/api/v1/command/$cmdId" `
                                     -Headers $headers -TimeoutSec 10
    } while ($status.status -notin @('completed','failed') -and (Get-Date) -lt $deadline)

    if ($status.status -ne 'completed') { throw "command status: $($status.status)" }

    # Locate the newest .zip under Backups/
    $backupDir = Join-Path $ReadarrRoot 'Backups'
    $newestZip = Get-ChildItem $backupDir -Filter '*.zip' -Recurse -ErrorAction SilentlyContinue |
                  Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $newestZip) { throw 'no backup zip produced' }

    Emit-Result 'ApiZip' $newestZip.FullName
    exit 0
}
catch {
    # Any failure → fall back to file copy
    Invoke-FileCopyBackup
    exit 0
}
