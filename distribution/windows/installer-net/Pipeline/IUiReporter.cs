namespace Readarr.Installer.Pipeline;

public interface IUiReporter
{
    void SetStep(string step, string detail = "");
    void SetProgressPercent(double percent);
    void SetProgressIndeterminate();
    void Log(LogLine line);
    void Log(string line);
    void LogError(string line);
}
