using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Readarr.Installer.Pipeline;

public sealed record InstallOutcome(bool Success, string? BackupPath, string? ErrorMessage);

public static class InstallPipeline
{
    private static readonly TimeSpan StopServiceTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan VerifyRunningTimeout = TimeSpan.FromSeconds(60);

    public static async Task<InstallOutcome> RunUpgradeAsync(
        IUiReporter ui,
        CancellationToken ct = default)
    {
        var log = new Progress<LogLine>(line => ui.Log(line));
        var extractProgress = new Progress<ExtractProgress>(p =>
            ui.SetProgressPercent(p.Total == 0 ? 0 : p.Extracted * 100.0 / p.Total));

        string? backupPath = null;

        try
        {
            ui.SetStep("Checking existing installation...", string.Empty);
            ui.SetProgressIndeterminate();

            var hasExisting = BackupService.HasExistingInstall();
            ui.Log(hasExisting
                ? "[pipeline] Existing Readarr installation detected at " + BackupService.ReadarrRoot
                : "[pipeline] No prior installation detected — fresh install.");

            if (hasExisting)
            {
                ui.SetStep("Stopping Readarr service...", "Service must be stopped to back up the database safely.");
                var stopped = await ServiceLifecycle.StopAsync(log, StopServiceTimeout, ct).ConfigureAwait(false);
                if (!stopped)
                {
                    return Failure("Failed to stop the Readarr service; install aborted.", null);
                }

                ui.SetStep("Backing up existing installation...",
                    "Robocopy + SQLite integrity check. This may take a few minutes.");
                var backup = await BackupService.BackupAsync(log, ct).ConfigureAwait(false);
                backupPath = backup.Path;

                var previousVersion = BackupService.GetPreviousVersion();
                ui.Log($"[pipeline] Previous version: {previousVersion ?? "(unknown)"}");

                var rollbackScriptPath = Path.Combine(BackupService.ReadarrRoot, "bin", "upgrade-helpers", "rollback.ps1");
                RegistryService.WriteUpgradeStart(backup.Method, backup.Path, rollbackScriptPath, previousVersion);
                ui.Log("[pipeline] Registry upgrade record written.");

                ui.SetStep("Removing existing service...", string.Empty);
                await ServiceLifecycle.DeleteAsync(log, ct).ConfigureAwait(false);
            }

            ui.SetStep("Extracting new Readarr binaries...", "Replacing bin/ atomically.");
            ui.SetProgressPercent(0);
            await PayloadExtractor.ExtractAndSwapAsync(log, extractProgress, ct).ConfigureAwait(false);

            ui.SetStep("Installing Windows service...", "Readarr.Console.exe /i");
            ui.SetProgressIndeterminate();
            var consoleExe = Path.Combine(BackupService.ReadarrRoot, "bin", "Readarr.Console.exe");
            if (!File.Exists(consoleExe))
            {
                return Failure($"Expected Readarr.Console.exe at {consoleExe} after extract — not found.", backupPath);
            }

            var installExit = await ProcessStreamer.RunAsync(
                consoleExe, "/i /exitimmediately", Path.GetDirectoryName(consoleExe), log, ct).ConfigureAwait(false);
            if (installExit != 0)
            {
                ui.LogError($"[pipeline] Readarr.Console.exe /i exited {installExit}.");
            }

            ui.SetStep("Verifying service is running...",
                "Waiting up to 60 seconds for the Readarr service to report RUNNING.");
            var running = await ServiceLifecycle.PollForRunningAsync(log, VerifyRunningTimeout, ct).ConfigureAwait(false);

            if (running)
            {
                var newVersion = ReadInstalledReadarrVersion();
                if (!string.IsNullOrWhiteSpace(newVersion))
                {
                    RegistryService.WriteCurrentVersion(newVersion);
                }
                ui.SetStep("Installation complete.",
                    string.IsNullOrWhiteSpace(newVersion) ? "Readarr is running." : $"Readarr {newVersion} is running.");
                ui.SetProgressPercent(100);
                return new InstallOutcome(true, backupPath, null);
            }

            // Service didn't start — try auto-rollback if we have a backup to restore from.
            if (backupPath is null)
            {
                return Failure(
                    "The new Readarr service did not reach RUNNING. No pre-upgrade backup was taken (fresh install). " +
                    "Check the Windows Event Viewer for service start errors.",
                    null);
            }

            ui.SetStep("Rolling back...",
                "The new install failed to start. Restoring the previous version from backup.");
            ui.LogError("[pipeline] Service did not start — triggering auto-rollback.");
            await RollbackRunner.RunAsync(log, ct).ConfigureAwait(false);
            RegistryService.WriteRollbackTimestamp();

            return Failure(
                $"The new Readarr service did not reach RUNNING within {VerifyRunningTimeout.TotalSeconds:F0}s. " +
                $"Your previous installation has been restored from backup:\n\n{backupPath}",
                backupPath);
        }
        catch (OperationCanceledException)
        {
            ui.LogError("[pipeline] Cancelled by user.");
            return Failure("Cancelled.", backupPath);
        }
        catch (Exception ex)
        {
            ui.LogError($"[pipeline] {ex.GetType().Name}: {ex.Message}");
            return Failure(ex.Message, backupPath);
        }
    }

    public static async Task<InstallOutcome> RunRestoreAsync(IUiReporter ui, CancellationToken ct = default)
    {
        var log = new Progress<LogLine>(line => ui.Log(line));

        try
        {
            ui.SetStep("Restoring from the most recent pre-upgrade backup...", string.Empty);
            ui.SetProgressIndeterminate();

            var record = RegistryService.Read();
            if (record?.BackupPath is null)
            {
                return Failure("No upgrade record found in registry. Nothing to roll back.", null);
            }

            ui.Log($"[pipeline] Registry BackupMethod={record.BackupMethod}, BackupPath={record.BackupPath}");

            var exit = await RollbackRunner.RunAsync(log, ct).ConfigureAwait(false);
            if (exit != 0)
            {
                return Failure($"rollback.ps1 exited {exit}. See log above.", record.BackupPath);
            }

            RegistryService.WriteRollbackTimestamp();

            ui.SetStep("Restore complete.", "Previous Readarr version has been restored and the service started.");
            ui.SetProgressPercent(100);
            return new InstallOutcome(true, record.BackupPath, null);
        }
        catch (Exception ex)
        {
            ui.LogError($"[pipeline] {ex.GetType().Name}: {ex.Message}");
            return Failure(ex.Message, null);
        }
    }

    private static InstallOutcome Failure(string message, string? backupPath) =>
        new(false, backupPath, message);

    private static string? ReadInstalledReadarrVersion()
    {
        var exe = Path.Combine(BackupService.ReadarrRoot, "bin", "Readarr.exe");
        if (!File.Exists(exe))
        {
            return null;
        }
        try
        {
            return System.Diagnostics.FileVersionInfo.GetVersionInfo(exe).FileVersion;
        }
        catch
        {
            return null;
        }
    }
}
