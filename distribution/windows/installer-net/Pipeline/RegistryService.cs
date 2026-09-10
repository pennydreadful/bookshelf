using System;
using System.IO;
using Microsoft.Win32;

namespace Readarr.Installer.Pipeline;

public sealed record UpgradeRecord(
    string? LastUpgradeTime,
    string? BackupMethod,
    string? BackupPath,
    string? RollbackScript,
    string? PreviousVersion,
    string? CurrentVersion,
    string? LastRollbackTime);

public static class RegistryService
{
    private const string UpgradeSubKey = @"SOFTWARE\Readarr\Upgrade";

    public static UpgradeRecord? Read()
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = baseKey.OpenSubKey(UpgradeSubKey, writable: false);
        if (key is null)
        {
            return null;
        }

        return new UpgradeRecord(
            LastUpgradeTime: key.GetValue("LastUpgradeTime") as string,
            BackupMethod: key.GetValue("BackupMethod") as string,
            BackupPath: key.GetValue("BackupPath") as string,
            RollbackScript: key.GetValue("RollbackScript") as string,
            PreviousVersion: key.GetValue("PreviousVersion") as string,
            CurrentVersion: key.GetValue("CurrentVersion") as string,
            LastRollbackTime: key.GetValue("LastRollbackTime") as string);
    }

    public static bool HasRecoverableUpgrade()
    {
        var record = Read();
        if (record?.BackupPath is null || string.IsNullOrWhiteSpace(record.BackupPath))
        {
            return false;
        }

        return Directory.Exists(record.BackupPath) || File.Exists(record.BackupPath);
    }

    public static void WriteUpgradeStart(
        string backupMethod,
        string backupPath,
        string rollbackScript,
        string? previousVersion)
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = baseKey.CreateSubKey(UpgradeSubKey, writable: true);
        if (key is null)
        {
            throw new InvalidOperationException($"Unable to open or create registry key: HKLM\\{UpgradeSubKey}");
        }

        key.SetValue("LastUpgradeTime", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        key.SetValue("BackupMethod", backupMethod);
        key.SetValue("BackupPath", backupPath);
        key.SetValue("RollbackScript", rollbackScript);
        if (!string.IsNullOrWhiteSpace(previousVersion))
        {
            key.SetValue("PreviousVersion", previousVersion);
        }
    }

    public static void WriteCurrentVersion(string version)
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = baseKey.CreateSubKey(UpgradeSubKey, writable: true);
        if (key is null)
        {
            return;
        }

        key.SetValue("CurrentVersion", version);
    }

    public static void WriteRollbackTimestamp()
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = baseKey.CreateSubKey(UpgradeSubKey, writable: true);
        if (key is null)
        {
            return;
        }

        key.SetValue("LastRollbackTime", DateTime.UtcNow.ToString("o"));
    }
}
