using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Readarr.Installer.Pipeline;

public enum LogStream
{
    Stdout,
    Stderr
}

public readonly record struct LogLine(LogStream Stream, string Text);

public static class ProcessStreamer
{
    public static async Task<int> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory,
        IProgress<LogLine> log,
        CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            psi.WorkingDirectory = workingDirectory;
        }

        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

        proc.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                log.Report(new LogLine(LogStream.Stdout, e.Data));
            }
        };
        proc.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                log.Report(new LogLine(LogStream.Stderr, e.Data));
            }
        };

        if (!proc.Start())
        {
            throw new InvalidOperationException($"Failed to start process: {fileName}");
        }

        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        using (ct.Register(() =>
        {
            try
            {
                if (!proc.HasExited)
                {
                    proc.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Best effort — process may have already exited between the check and Kill.
            }
        }))
        {
            await proc.WaitForExitAsync(ct).ConfigureAwait(false);
        }

        return proc.ExitCode;
    }
}
