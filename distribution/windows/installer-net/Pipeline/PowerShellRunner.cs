using System;
using System.Threading;
using System.Threading.Tasks;

namespace Readarr.Installer.Pipeline;

public static class PowerShellRunner
{
    public static Task<int> RunScriptAsync(
        string scriptPath,
        string? scriptArgs,
        IProgress<LogLine> log,
        CancellationToken ct = default)
    {
        var args = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{scriptPath}\"";
        if (!string.IsNullOrWhiteSpace(scriptArgs))
        {
            args += " " + scriptArgs;
        }

        return ProcessStreamer.RunAsync(
            fileName: "powershell.exe",
            arguments: args,
            workingDirectory: null,
            log: log,
            ct: ct);
    }
}
