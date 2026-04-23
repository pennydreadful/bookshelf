using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Readarr.Installer.Pipeline;

public sealed record BackupResult(string Method, string Path);

public static class BackupService
{
    public const string ReadarrRoot = @"C:\ProgramData\Readarr";
    public const string BackupRoot = @"C:\ProgramData\Readarr.backups";

    private const int KeepMostRecent = 2;

    public static async Task<BackupResult> BackupAsync(IProgress<LogLine> log, CancellationToken ct = default)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var target = Path.Combine(BackupRoot, timestamp);

        log.Report(new LogLine(LogStream.Stdout, $"[backup] Target: {target}"));
        Directory.CreateDirectory(target);

        await CopyBinAsync(target, log, ct).ConfigureAwait(false);
        CopySqliteSet(target, log);
        CopySingleFile(Path.Combine(ReadarrRoot, "config.xml"), target, log);
        CopyBackupsFolder(target, log);
        StripInstallerArtifacts(target, log);

        RunIntegrityCheck(target, log);

        RotateOldBackups(log);

        log.Report(new LogLine(LogStream.Stdout, $"[backup] Complete: {target}"));
        return new BackupResult("FileCopy", target);
    }

    private static async Task CopyBinAsync(string target, IProgress<LogLine> log, CancellationToken ct)
    {
        var src = Path.Combine(ReadarrRoot, "bin");
        if (!Directory.Exists(src))
        {
            log.Report(new LogLine(LogStream.Stdout, "[backup] No bin/ to copy (fresh install?)."));
            return;
        }

        var fileCount = Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories).Count();
        var sizeBytes = Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories)
            .Sum(f => new FileInfo(f).Length);
        var sizeMb = Math.Round(sizeBytes / 1024.0 / 1024.0);

        log.Report(new LogLine(LogStream.Stdout, $"[backup] Copying binaries ({fileCount} files, ~{sizeMb:F0} MB)..."));

        var destBin = Path.Combine(target, "bin");
        // robocopy: /E=copy subdirs including empty, /MT:8=8 threads, /NFL /NDL /NC /NS /NP = minimal stdout
        var args = $"\"{src}\" \"{destBin}\" /E /MT:8 /NFL /NDL /NC /NS /NP";
        var exit = await ProcessStreamer.RunAsync("robocopy.exe", args, null, log, ct).ConfigureAwait(false);

        // robocopy exit codes: 0-7 are success (bit flags). 8+ indicate failure.
        if (exit >= 8)
        {
            throw new InvalidOperationException($"robocopy failed with exit code {exit} — backup aborted.");
        }
    }

    private static void CopySqliteSet(string target, IProgress<LogLine> log)
    {
        var set = new[]
        {
            "readarr.db",
            "readarr.db-journal",
            "readarr.db-wal",
            "readarr.db-shm"
        };

        var primary = Path.Combine(ReadarrRoot, "readarr.db");
        if (File.Exists(primary))
        {
            var dbMb = Math.Round(new FileInfo(primary).Length / 1024.0 / 1024.0);
            log.Report(new LogLine(LogStream.Stdout, $"[backup] Copying database ({dbMb:F0} MB)..."));
        }

        foreach (var name in set)
        {
            var src = Path.Combine(ReadarrRoot, name);
            if (!File.Exists(src))
            {
                continue;
            }

            var dst = Path.Combine(target, name);
            File.Copy(src, dst, overwrite: true);
        }
    }

    private static void CopySingleFile(string src, string targetDir, IProgress<LogLine> log)
    {
        if (!File.Exists(src))
        {
            return;
        }

        var dst = Path.Combine(targetDir, Path.GetFileName(src));
        File.Copy(src, dst, overwrite: true);
        log.Report(new LogLine(LogStream.Stdout, $"[backup] Copied {Path.GetFileName(src)}."));
    }

    private static void CopyBackupsFolder(string target, IProgress<LogLine> log)
    {
        var src = Path.Combine(ReadarrRoot, "Backups");
        if (!Directory.Exists(src))
        {
            return;
        }

        log.Report(new LogLine(LogStream.Stdout, "[backup] Copying existing Readarr backups..."));
        var dst = Path.Combine(target, "Backups");
        CopyDirectoryRecursive(src, dst);
    }

    private static void CopyDirectoryRecursive(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var file in Directory.EnumerateFiles(src))
        {
            var name = Path.GetFileName(file);
            File.Copy(file, Path.Combine(dst, name), overwrite: true);
        }
        foreach (var sub in Directory.EnumerateDirectories(src))
        {
            var name = Path.GetFileName(sub);
            CopyDirectoryRecursive(sub, Path.Combine(dst, name));
        }
    }

    private static void StripInstallerArtifacts(string target, IProgress<LogLine> log)
    {
        log.Report(new LogLine(LogStream.Stdout, "[backup] Stripping installer artifacts from copied bin/..."));
        var bin = Path.Combine(target, "bin");

        var rollbackBat = Path.Combine(bin, "Rollback-To-Pre-Upgrade.bat");
        if (File.Exists(rollbackBat))
        {
            File.Delete(rollbackBat);
        }

        var upgradeHelpers = Path.Combine(bin, "upgrade-helpers");
        if (Directory.Exists(upgradeHelpers))
        {
            Directory.Delete(upgradeHelpers, recursive: true);
        }
    }

    private static void RunIntegrityCheck(string target, IProgress<LogLine> log)
    {
        var db = Path.Combine(target, "readarr.db");
        if (!File.Exists(db))
        {
            log.Report(new LogLine(LogStream.Stdout, "[backup] No readarr.db to verify; skipping integrity check."));
            return;
        }

        log.Report(new LogLine(LogStream.Stdout, "[backup] Running SQLite PRAGMA integrity_check on copied DB..."));

        // Mode=ReadOnly so the check doesn't touch the copy; default journal mode.
        var cs = new SqliteConnectionStringBuilder
        {
            DataSource = db,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Private
        }.ToString();

        try
        {
            using var conn = new SqliteConnection(cs);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            using var reader = cmd.ExecuteReader();
            var firstResult = reader.Read() ? reader.GetString(0) : "(no output)";

            if (string.Equals(firstResult, "ok", StringComparison.OrdinalIgnoreCase))
            {
                log.Report(new LogLine(LogStream.Stdout, "[backup] integrity_check: ok"));
            }
            else
            {
                log.Report(new LogLine(LogStream.Stderr, $"[backup] integrity_check returned: {firstResult}"));
                throw new InvalidOperationException($"SQLite integrity_check failed on backup copy: {firstResult}");
            }
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException($"SQLite integrity check error: {ex.Message}", ex);
        }
    }

    private static void RotateOldBackups(IProgress<LogLine> log)
    {
        if (!Directory.Exists(BackupRoot))
        {
            return;
        }

        var dirs = new DirectoryInfo(BackupRoot)
            .GetDirectories()
            .OrderByDescending(d => d.LastWriteTimeUtc)
            .Skip(KeepMostRecent)
            .ToList();

        if (dirs.Count == 0)
        {
            return;
        }

        log.Report(new LogLine(LogStream.Stdout, $"[backup] Rotating — deleting {dirs.Count} old backup(s)..."));
        foreach (var dir in dirs)
        {
            try
            {
                dir.Delete(recursive: true);
                log.Report(new LogLine(LogStream.Stdout, $"[backup]   Removed: {dir.Name}"));
            }
            catch (Exception ex)
            {
                log.Report(new LogLine(LogStream.Stderr, $"[backup]   Failed to remove {dir.Name}: {ex.Message}"));
            }
        }
    }

    public static string? GetPreviousVersion()
    {
        var exe = Path.Combine(ReadarrRoot, "bin", "Readarr.exe");
        if (!File.Exists(exe))
        {
            return null;
        }

        try
        {
            var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(exe);
            return info.FileVersion;
        }
        catch
        {
            return null;
        }
    }

    public static bool HasExistingInstall()
    {
        return File.Exists(Path.Combine(ReadarrRoot, "config.xml"));
    }
}
