using System;
using System.Windows;

namespace Readarr.Installer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var restoreMode = false;
        foreach (var arg in e.Args)
        {
            if (string.Equals(arg, "/restore", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--restore", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-restore", StringComparison.OrdinalIgnoreCase))
            {
                restoreMode = true;
            }
        }

        var window = new MainWindow(restoreMode);
        window.Show();
    }
}
