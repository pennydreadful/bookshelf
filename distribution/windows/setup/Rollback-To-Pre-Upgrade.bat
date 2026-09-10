@echo off
echo ============================================================
echo  Readarr Rollback - Restore to Pre-Upgrade State
echo ============================================================
echo.
echo This will:
echo   1. Stop the Readarr service
echo   2. Restore binaries and data from the most recent pre-upgrade backup
echo   3. Start the Readarr service
echo.
set /p confirm="Continue? (Y/N): "
if /i not "%confirm%"=="Y" (
    echo Cancelled.
    pause
    exit /b 0
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0upgrade-helpers\rollback.ps1"

echo.
pause
