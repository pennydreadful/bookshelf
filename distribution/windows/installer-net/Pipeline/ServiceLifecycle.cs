using System;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace Readarr.Installer.Pipeline;

public static class ServiceLifecycle
{
    public const string ServiceName = "Readarr";

    public static bool IsInstalled()
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            _ = sc.Status;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static ServiceControllerStatus? TryGetStatus()
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            return sc.Status;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public static async Task<bool> StopAsync(
        IProgress<LogLine> log,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        if (!IsInstalled())
        {
            log.Report(new LogLine(LogStream.Stdout, $"[service] {ServiceName} is not installed; nothing to stop."));
            return true;
        }

        using var sc = new ServiceController(ServiceName);
        try
        {
            sc.Refresh();
            if (sc.Status == ServiceControllerStatus.Stopped)
            {
                log.Report(new LogLine(LogStream.Stdout, $"[service] {ServiceName} already stopped."));
                return true;
            }

            log.Report(new LogLine(LogStream.Stdout, $"[service] Stopping {ServiceName}..."));
            if (sc.Status != ServiceControllerStatus.StopPending)
            {
                sc.Stop();
            }

            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();
                sc.Refresh();
                if (sc.Status == ServiceControllerStatus.Stopped)
                {
                    log.Report(new LogLine(LogStream.Stdout, $"[service] {ServiceName} stopped."));
                    return true;
                }
                await Task.Delay(500, ct).ConfigureAwait(false);
            }

            log.Report(new LogLine(LogStream.Stderr, $"[service] {ServiceName} did not stop within {timeout.TotalSeconds:F0}s."));
            return false;
        }
        catch (Exception ex)
        {
            log.Report(new LogLine(LogStream.Stderr, $"[service] Error stopping {ServiceName}: {ex.Message}"));
            return false;
        }
    }

    public static async Task<bool> DeleteAsync(IProgress<LogLine> log, CancellationToken ct = default)
    {
        if (!IsInstalled())
        {
            log.Report(new LogLine(LogStream.Stdout, $"[service] {ServiceName} not installed; skipping delete."));
            return true;
        }

        log.Report(new LogLine(LogStream.Stdout, $"[service] Deleting service {ServiceName}..."));
        var exit = await ProcessStreamer.RunAsync("sc.exe", $"delete {ServiceName}", null, log, ct).ConfigureAwait(false);
        if (exit != 0)
        {
            log.Report(new LogLine(LogStream.Stderr, $"[service] sc delete exited {exit}."));
            return false;
        }

        // sc delete is eventually consistent — the service record may take a moment to disappear.
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            if (!IsInstalled())
            {
                return true;
            }
            await Task.Delay(250, ct).ConfigureAwait(false);
        }

        return !IsInstalled();
    }

    public static async Task<bool> PollForRunningAsync(
        IProgress<LogLine> log,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        log.Report(new LogLine(LogStream.Stdout, $"[service] Waiting for {ServiceName} to reach RUNNING ({timeout.TotalSeconds:F0}s timeout)..."));
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            var status = TryGetStatus();
            if (status == ServiceControllerStatus.Running)
            {
                log.Report(new LogLine(LogStream.Stdout, $"[service] {ServiceName} is RUNNING."));
                return true;
            }

            await Task.Delay(2000, ct).ConfigureAwait(false);
        }

        log.Report(new LogLine(LogStream.Stderr, $"[service] {ServiceName} did not reach RUNNING within {timeout.TotalSeconds:F0}s."));
        return false;
    }
}
