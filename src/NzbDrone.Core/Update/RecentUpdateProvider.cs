using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Update
{
    public interface IRecentUpdateProvider
    {
        List<UpdatePackage> GetRecentUpdatePackages();
    }

    public class RecentUpdateProvider : IRecentUpdateProvider
    {
        private const int MaxUpdates = 5;

        private readonly IConfigFileProvider _configFileProvider;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;

        public RecentUpdateProvider(IConfigFileProvider configFileProvider,
                                    IAppFolderInfo appFolderInfo,
                                    IDiskProvider diskProvider)
        {
            _configFileProvider = configFileProvider;
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
        }

        public List<UpdatePackage> GetRecentUpdatePackages()
        {
            var updates = GetRecentUpdatesFromChangelog();

            return updates;
        }

        private List<UpdatePackage> GetRecentUpdatesFromChangelog()
        {
            var changelogPath = FindChangelogPath();
            if (string.IsNullOrWhiteSpace(changelogPath))
            {
                return new List<UpdatePackage>();
            }

            var entries = ParseChangelog(changelogPath);
            if (entries.Count == 0)
            {
                return new List<UpdatePackage>();
            }

            var branch = _configFileProvider.Branch;
            var now = DateTime.UtcNow;
            var packages = new List<UpdatePackage>();

            for (var index = 0; index < entries.Count && packages.Count < MaxUpdates; index++)
            {
                var entry = entries[index];
                var changes = new UpdateChanges();

                if (!string.IsNullOrWhiteSpace(entry.Summary))
                {
                    changes.New.Add(entry.Summary);
                }

                if (!string.IsNullOrWhiteSpace(entry.Impact))
                {
                    changes.Fixed.Add(entry.Impact);
                }

                packages.Add(new UpdatePackage
                {
                    Version = entry.Version,
                    Branch = branch,
                    ReleaseDate = now.AddDays(-index),
                    Changes = changes
                });
            }

            return packages;
        }

        private List<ChangelogEntry> ParseChangelog(string changelogPath)
        {
            if (!_diskProvider.FileExists(changelogPath))
            {
                return new List<ChangelogEntry>();
            }

            var lines = _diskProvider.ReadAllLines(changelogPath);
            var entries = new List<ChangelogEntry>();
            ChangelogEntry current = null;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    var versionText = line.Substring(3).Trim();

                    if (Version.TryParse(versionText, out var version))
                    {
                        current = new ChangelogEntry(version);
                        entries.Add(current);
                    }
                    else
                    {
                        current = null;
                    }

                    continue;
                }

                if (current == null || !line.StartsWith("- ", StringComparison.Ordinal))
                {
                    continue;
                }

                var item = line.Substring(2).Trim();

                if (item.StartsWith("Summary:", StringComparison.OrdinalIgnoreCase))
                {
                    current.Summary = item.Substring("Summary:".Length).Trim();
                }
                else if (item.StartsWith("Impact:", StringComparison.OrdinalIgnoreCase))
                {
                    current.Impact = item.Substring("Impact:".Length).Trim();
                }
            }

            return entries;
        }

        private string FindChangelogPath()
        {
            var candidates = new List<string>();
            var bookdarrHome = Environment.GetEnvironmentVariable("BOOKDARR_HOME");

            if (!string.IsNullOrWhiteSpace(bookdarrHome))
            {
                candidates.Add(Path.Combine(bookdarrHome, "CHANGELOG.md"));
            }

            var startUpFolder = _appFolderInfo.StartUpFolder;
            if (!string.IsNullOrWhiteSpace(startUpFolder))
            {
                var folder = new DirectoryInfo(startUpFolder);

                for (var i = 0; i < 6 && folder != null; i++)
                {
                    candidates.Add(Path.Combine(folder.FullName, "CHANGELOG.md"));
                    folder = folder.Parent;
                }
            }

            foreach (var candidate in candidates.Distinct())
            {
                if (_diskProvider.FileExists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private class ChangelogEntry
        {
            public ChangelogEntry(Version version)
            {
                Version = version;
            }

            public Version Version { get; }
            public string Summary { get; set; }
            public string Impact { get; set; }
        }
    }
}
