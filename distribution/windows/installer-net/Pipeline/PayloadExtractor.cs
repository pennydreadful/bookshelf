using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Readarr.Installer.Pipeline;

public sealed record ExtractProgress(int Extracted, int Total);

public static class PayloadExtractor
{
    private const string PayloadResourceName = "Readarr.Installer.Payload.Readarr.payload.zip";

    public static bool HasEmbeddedPayload()
    {
        return Assembly.GetExecutingAssembly().GetManifestResourceNames().Any(n =>
            string.Equals(n, PayloadResourceName, StringComparison.Ordinal));
    }

    public static async Task ExtractAndSwapAsync(
        IProgress<LogLine> log,
        IProgress<ExtractProgress> progress,
        CancellationToken ct = default)
    {
        var readarrRoot = BackupService.ReadarrRoot;
        Directory.CreateDirectory(readarrRoot);

        var binDir = Path.Combine(readarrRoot, "bin");
        var updateDir = Path.Combine(readarrRoot, "bin.update");
        var oldDir = Path.Combine(readarrRoot, "bin.old");

        // Start clean — any prior aborted attempt could have left a .update or .old around.
        if (Directory.Exists(updateDir))
        {
            log.Report(new LogLine(LogStream.Stdout, $"[payload] Removing stale {updateDir}..."));
            Directory.Delete(updateDir, recursive: true);
        }
        if (Directory.Exists(oldDir))
        {
            log.Report(new LogLine(LogStream.Stdout, $"[payload] Removing stale {oldDir}..."));
            Directory.Delete(oldDir, recursive: true);
        }

        Directory.CreateDirectory(updateDir);

        await ExtractEmbeddedZipAsync(updateDir, log, progress, ct).ConfigureAwait(false);

        log.Report(new LogLine(LogStream.Stdout, "[payload] Swapping bin/ with new payload (atomic rename)..."));
        if (Directory.Exists(binDir))
        {
            Directory.Move(binDir, oldDir);
        }
        Directory.Move(updateDir, binDir);

        if (Directory.Exists(oldDir))
        {
            log.Report(new LogLine(LogStream.Stdout, "[payload] Removing previous bin.old..."));
            try
            {
                Directory.Delete(oldDir, recursive: true);
            }
            catch (Exception ex)
            {
                // Non-fatal: the new binaries are in place; leftover bin.old can be cleaned later.
                log.Report(new LogLine(LogStream.Stderr, $"[payload] Could not remove bin.old: {ex.Message}"));
            }
        }

        log.Report(new LogLine(LogStream.Stdout, $"[payload] Payload extracted to {binDir}."));
    }

    private static async Task ExtractEmbeddedZipAsync(
        string destDir,
        IProgress<LogLine> log,
        IProgress<ExtractProgress> progress,
        CancellationToken ct)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream(PayloadResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Embedded payload resource '{PayloadResourceName}' not found. " +
                $"Did the build step drop Payload\\Readarr.payload.zip before publish?");
        }

        log.Report(new LogLine(LogStream.Stdout, "[payload] Opening embedded payload archive..."));
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var totalEntries = archive.Entries.Count(e => !string.IsNullOrEmpty(e.Name));
        log.Report(new LogLine(LogStream.Stdout, $"[payload] Extracting {totalEntries} files..."));

        var extracted = 0;
        foreach (var entry in archive.Entries)
        {
            ct.ThrowIfCancellationRequested();

            var destPath = Path.Combine(destDir, entry.FullName);
            var destSubDir = Path.GetDirectoryName(destPath);
            if (destSubDir != null)
            {
                Directory.CreateDirectory(destSubDir);
            }

            // Directory entries have empty Name; skip after creating the directory above.
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            entry.ExtractToFile(destPath, overwrite: true);
            extracted++;

            if (extracted % 25 == 0 || extracted == totalEntries)
            {
                progress.Report(new ExtractProgress(extracted, totalEntries));
            }
        }

        progress.Report(new ExtractProgress(totalEntries, totalEntries));
        log.Report(new LogLine(LogStream.Stdout, $"[payload] Extracted {extracted}/{totalEntries} files."));
        await Task.CompletedTask;
    }
}
