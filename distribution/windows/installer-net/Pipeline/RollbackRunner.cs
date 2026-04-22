using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Readarr.Installer.Pipeline;

public static class RollbackRunner
{
    private const string ResourceName = "rollback.ps1";

    public static async Task<int> RunAsync(IProgress<LogLine> log, CancellationToken ct = default)
    {
        var tempPath = ExtractToTemp();
        try
        {
            log.Report(new LogLine(LogStream.Stdout, $"[rollback] Running {tempPath}..."));
            var exit = await PowerShellRunner.RunScriptAsync(tempPath, null, log, ct).ConfigureAwait(false);
            log.Report(new LogLine(LogStream.Stdout, $"[rollback] rollback.ps1 exited {exit}."));
            return exit;
        }
        finally
        {
            try { File.Delete(tempPath); }
            catch { /* Best effort cleanup; a leftover temp file is not worth escalating. */ }
        }
    }

    private static string ExtractToTemp()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found in installer.");

        var tempDir = Path.Combine(Path.GetTempPath(), "Readarr.Installer");
        Directory.CreateDirectory(tempDir);
        var tempPath = Path.Combine(tempDir, $"rollback-{Guid.NewGuid():N}.ps1");

        using var fs = File.Create(tempPath);
        stream.CopyTo(fs);
        return tempPath;
    }
}
