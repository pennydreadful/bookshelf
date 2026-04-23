# Restores Readarr from a pre-upgrade backup recorded in the Windows Registry.
# Reads HKLM\SOFTWARE\Readarr\Upgrade\{BackupMethod, BackupPath} to locate the backup.
# Works for both ApiZip (native Readarr backup) and FileCopy (raw tree copy) methods.

$ErrorActionPreference = 'Stop'

$ReadarrRoot = 'C:\ProgramData\Readarr'

# Check both registry views. Inno Setup installers that run as 32-bit
# processes write to HKLM\SOFTWARE\WOW6432Node\... under WOW64 redirection,
# while 64-bit PowerShell reads the native view. Scan both so the script
# works regardless of which view carries the data.
$RegPathCandidates = @(
    'HKLM:\SOFTWARE\Readarr\Upgrade',
    'HKLM:\SOFTWARE\WOW6432Node\Readarr\Upgrade'
)

$RegPath = $null
$method = $null
$path = $null
foreach ($candidate in $RegPathCandidates) {
    try {
        $props = Get-ItemProperty -Path $candidate -ErrorAction Stop
        if ($props.BackupMethod -and $props.BackupPath) {
            $RegPath = $candidate
            $method = $props.BackupMethod
            $path = $props.BackupPath
            Write-Host "Found upgrade record: $candidate"
            break
        }
    } catch { }
}

if (-not $method -or -not $path) {
    Write-Host 'No upgrade record found in registry. Nothing to roll back.' -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $path)) {
    Write-Host "Backup path not found on disk: $path" -ForegroundColor Red
    Write-Host 'Cannot roll back.' -ForegroundColor Red
    exit 1
}

Write-Host "Pre-upgrade backup: $path"
Write-Host "Method: $method"
Write-Host ''

Write-Host 'Stopping Readarr service...'
Stop-Service -Name Readarr -Force -ErrorAction SilentlyContinue
$deadline = (Get-Date).AddSeconds(30)
while ((Get-Service -Name Readarr -ErrorAction SilentlyContinue).Status -ne 'Stopped' -and (Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 1
}

Write-Host 'Restoring files...'
if ($method -eq 'ApiZip') {
    # Extract the Readarr-native zip over the install root (config.xml, readarr.db, etc. — no binaries)
    Expand-Archive -Path $path -DestinationPath $ReadarrRoot -Force
}
elseif ($method -eq 'FileCopy') {
    # Copy backup dir contents back in place.
    # Defensive: skip installer-authored artifacts (Rollback-To-Pre-Upgrade.bat,
    # upgrade-helpers/) from old backups where do-backup.ps1 didn't strip them.
    # Overwriting the running .bat causes cmd.exe to bail without running 'pause'.
    Get-ChildItem -LiteralPath $path | ForEach-Object {
        if ($_.Name -eq 'bin') {
            $destBin = Join-Path $ReadarrRoot 'bin'
            if (-not (Test-Path $destBin)) { New-Item -ItemType Directory -Path $destBin -Force | Out-Null }
            Get-ChildItem -LiteralPath $_.FullName | ForEach-Object {
                if ($_.Name -eq 'Rollback-To-Pre-Upgrade.bat') { return }
                if ($_.Name -eq 'upgrade-helpers') { return }
                Copy-Item -Recurse -Force -Path $_.FullName -Destination $destBin
            }
        }
        else {
            Copy-Item -Recurse -Force -Path $_.FullName -Destination $ReadarrRoot
        }
    }
}
else {
    Write-Host "Unknown backup method: $method" -ForegroundColor Red
    exit 1
}

Write-Host 'Starting Readarr service...'
Start-Service -Name Readarr -ErrorAction SilentlyContinue

# Record rollback
try {
    Set-ItemProperty -Path $RegPath -Name LastRollbackTime -Value (Get-Date -Format 'o')
} catch { }

Write-Host ''
Write-Host 'Rollback complete.' -ForegroundColor Green
