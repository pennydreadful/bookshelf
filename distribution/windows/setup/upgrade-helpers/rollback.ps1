# Restores Readarr from a pre-upgrade backup recorded in the Windows Registry.
# Reads HKLM\SOFTWARE\Readarr\Upgrade\{BackupMethod, BackupPath} to locate the backup.
# Works for both ApiZip (native Readarr backup) and FileCopy (raw tree copy) methods.

$ErrorActionPreference = 'Stop'

$ReadarrRoot = 'C:\ProgramData\Readarr'
$RegPath = 'HKLM:\SOFTWARE\Readarr\Upgrade'

try {
    $props = Get-ItemProperty -Path $RegPath
    $method = $props.BackupMethod
    $path = $props.BackupPath
    if (-not $method -or -not $path) { throw 'missing registry values' }
} catch {
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
    # Extract the Readarr-native zip over the install root
    Expand-Archive -Path $path -DestinationPath $ReadarrRoot -Force
}
elseif ($method -eq 'FileCopy') {
    # Copy backup dir contents back in place
    Get-ChildItem -LiteralPath $path | ForEach-Object {
        Copy-Item -Recurse -Force -Path $_.FullName -Destination $ReadarrRoot
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
