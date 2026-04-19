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

function Emit-Result([string]$method, [string]$path) {
    Write-Output (@{method = $method; path = $path} | ConvertTo-Json -Compress)
}

function Invoke-FileCopyBackup {
    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $target = Join-Path $BackupRoot $timestamp
    New-Item -ItemType Directory -Path $target -Force | Out-Null

    $itemsToBackup = @('bin', 'config.xml',
                       'readarr.db', 'readarr.db-journal', 'readarr.db-wal', 'readarr.db-shm',
                       'Backups')
    foreach ($item in $itemsToBackup) {
        $src = Join-Path $ReadarrRoot $item
        if (Test-Path $src) {
            Copy-Item -Recurse -Force -Path $src -Destination $target -ErrorAction SilentlyContinue
        }
    }

    # Rotation: keep only 2 newest backup dirs
    Get-ChildItem $BackupRoot -Directory -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -Skip 2 |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

    Emit-Result 'FileCopy' $target
}

# Try API-based backup
try {
    if (-not (Test-Path $ConfigFile)) { throw 'config.xml missing' }
    $cfg = [xml](Get-Content $ConfigFile -Raw)
    $port = $cfg.Config.Port
    $apiKey = $cfg.Config.ApiKey
    if (-not $port -or -not $apiKey) { throw 'port/apikey missing' }

    $headers = @{'X-Api-Key' = $apiKey}
    $body = @{name = 'Backup'} | ConvertTo-Json

    $cmd = Invoke-RestMethod -Method POST `
                              -Uri "http://localhost:$port/api/v1/command" `
                              -Headers $headers -Body $body `
                              -ContentType 'application/json' -TimeoutSec 10
    $cmdId = $cmd.id

    # Poll up to 5 min
    $deadline = (Get-Date).AddMinutes(5)
    do {
        Start-Sleep -Seconds 2
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
