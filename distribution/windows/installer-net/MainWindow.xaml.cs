using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Readarr.Installer.Pipeline;

namespace Readarr.Installer;

public partial class MainWindow : IUiReporter
{
    private bool _autoScroll = true;
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private readonly bool _restoreMode;

    public MainWindow() : this(restoreMode: false) { }

    public MainWindow(bool restoreMode)
    {
        _restoreMode = restoreMode;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_restoreMode)
        {
            Title = "Readarr Installer — Restore";
            InstallButton.Visibility = Visibility.Collapsed;
            RestoreButton.Visibility = Visibility.Collapsed;
            AppendLog("[installer] /restore mode — running rollback.ps1 against the most recent pre-upgrade backup.");
            await RunRestoreAsync().ConfigureAwait(true);
            return;
        }

        // Offer the Restore button if there's a recoverable upgrade record in the registry.
        if (RegistryService.HasRecoverableUpgrade())
        {
            var record = RegistryService.Read();
            RestoreButton.Visibility = Visibility.Visible;
            AppendLog("[installer] Prior upgrade record detected — Restore option available.");
            if (!string.IsNullOrWhiteSpace(record?.PreviousVersion))
            {
                AppendLog($"[installer]   PreviousVersion: {record.PreviousVersion}");
            }
            if (!string.IsNullOrWhiteSpace(record?.BackupPath))
            {
                AppendLog($"[installer]   BackupPath: {record.BackupPath}");
            }
        }
    }

    private async Task RunRestoreAsync()
    {
        _isRunning = true;
        _cts = new CancellationTokenSource();
        SetButtonsForRunning();

        try
        {
            var outcome = await Task.Run(() => InstallPipeline.RunRestoreAsync(this, _cts.Token))
                .ConfigureAwait(true);

            if (!outcome.Success)
            {
                AppendLogColored($"[installer] Restore failed: {outcome.ErrorMessage}", Color.FromRgb(0xE8, 0x4A, 0x4A));
                MessageBox.Show(this,
                    outcome.ErrorMessage ?? "Restore failed.",
                    "Readarr Installer",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            _isRunning = false;
            _cts?.Dispose();
            _cts = null;
            SetButtonsForDone();
        }
    }

    // --- IUiReporter (always marshal to UI thread) ---

    public void SetStep(string step, string detail = "")
    {
        Dispatcher.Invoke(() =>
        {
            StepLabel.Text = step;
            DetailLabel.Text = detail;
        });
    }

    public void SetProgressPercent(double percent)
    {
        Dispatcher.Invoke(() =>
        {
            Progress.IsIndeterminate = false;
            Progress.Value = Math.Clamp(percent, 0, 100);
        });
    }

    public void SetProgressIndeterminate()
    {
        Dispatcher.Invoke(() => Progress.IsIndeterminate = true);
    }

    public void Log(LogLine line)
    {
        if (line.Stream == LogStream.Stderr)
        {
            LogError(line.Text);
        }
        else
        {
            Log(line.Text);
        }
    }

    public void Log(string line) => Dispatcher.Invoke(() => AppendLog(line));
    public void LogError(string line) => Dispatcher.Invoke(() => AppendLogColored(line, Color.FromRgb(0xE8, 0x4A, 0x4A)));

    // --- Internal log rendering ---

    private void AppendLog(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return;
        }

        LogText.Inlines.Add(new Run(line));
        LogText.Inlines.Add(new LineBreak());

        if (_autoScroll)
        {
            LogScroller.ScrollToEnd();
        }
    }

    private void AppendLogColored(string line, Color color)
    {
        if (string.IsNullOrEmpty(line))
        {
            return;
        }

        LogText.Inlines.Add(new Run(line) { Foreground = new SolidColorBrush(color) });
        LogText.Inlines.Add(new LineBreak());

        if (_autoScroll)
        {
            LogScroller.ScrollToEnd();
        }
    }

    private void LogScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (Math.Abs(e.ExtentHeightChange) < 0.5)
        {
            // User-initiated scroll — update auto-scroll state based on whether they're at the bottom.
            _autoScroll = e.VerticalOffset >= LogScroller.ScrollableHeight - 2;
        }
    }

    // --- Button state machine ---

    private void SetButtonsForIdle()
    {
        InstallButton.IsEnabled = true;
        CancelButton.IsEnabled = false;
        CloseButton.IsEnabled = true;
    }

    private void SetButtonsForRunning()
    {
        InstallButton.IsEnabled = false;
        RestoreButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        CloseButton.IsEnabled = false;
    }

    private void SetButtonsForDone()
    {
        InstallButton.IsEnabled = false;
        RestoreButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        CloseButton.IsEnabled = true;
    }

    // --- Button handlers ---

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning) return;
        _isRunning = true;
        _cts = new CancellationTokenSource();
        SetButtonsForRunning();

        try
        {
            var outcome = await Task.Run(() => InstallPipeline.RunUpgradeAsync(this, _cts.Token))
                .ConfigureAwait(true);

            if (!outcome.Success)
            {
                AppendLogColored($"[installer] FAILED: {outcome.ErrorMessage}", Color.FromRgb(0xE8, 0x4A, 0x4A));
                if (!string.IsNullOrWhiteSpace(outcome.BackupPath))
                {
                    AppendLog($"[installer] Backup preserved at: {outcome.BackupPath}");
                }
                MessageBox.Show(this,
                    outcome.ErrorMessage ?? "Installation failed.",
                    "Readarr Installer",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            _isRunning = false;
            _cts?.Dispose();
            _cts = null;
            SetButtonsForDone();
        }
    }

    private async void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning) return;

        var confirm = MessageBox.Show(this,
            "Restore Readarr from the most recent pre-upgrade backup? The Readarr service will be restarted.",
            "Confirm restore",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.OK) return;

        await RunRestoreAsync().ConfigureAwait(true);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_cts is null) return;
        AppendLogColored("[installer] Cancel requested — attempting graceful stop.", Colors.Goldenrod);
        _cts.Cancel();
        CancelButton.IsEnabled = false;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
