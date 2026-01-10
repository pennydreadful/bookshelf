using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Diagnostics
{
    public interface IDiagnosticsPushService
    {
        DiagnosticsStatus GetStatus();
        DiagnosticsPushResult PushDiagnostics();
        void AppendUiEvents(IEnumerable<string> eventsJson);
    }

    public class DiagnosticsStatus
    {
        public bool IsDevelop { get; set; }
        public bool HasToken { get; set; }
        public string Repo { get; set; }
    }

    public class DiagnosticsPushResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Commit { get; set; }
        public string Folder { get; set; }
    }

    public class DiagnosticsPushService : IDiagnosticsPushService
    {
        private static readonly string[] AllowedExtensions = { ".log", ".txt", ".json", ".xml" };
        private static readonly Regex SensitiveXmlElement = new Regex("^(ApiKey|DiagnosticsToken|PostgresPassword|SslCertPassword)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly object UiEventsLock = new object();

        private readonly IConfigFileProvider _configFileProvider;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;
        private readonly IProcessProvider _processProvider;
        private readonly Logger _logger;

        public DiagnosticsPushService(IConfigFileProvider configFileProvider,
                                      IAppFolderInfo appFolderInfo,
                                      IDiskProvider diskProvider,
                                      IProcessProvider processProvider,
                                      Logger logger)
        {
            _configFileProvider = configFileProvider;
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
            _processProvider = processProvider;
            _logger = logger;
        }

        public DiagnosticsStatus GetStatus()
        {
            return new DiagnosticsStatus
            {
                IsDevelop = IsDevelopBranch(),
                HasToken = _configFileProvider.DiagnosticsToken.IsNotNullOrWhiteSpace(),
                Repo = _configFileProvider.DiagnosticsRepo
            };
        }

        public void AppendUiEvents(IEnumerable<string> eventsJson)
        {
            if (!IsDevelopBranch())
            {
                return;
            }

            var logFolder = _appFolderInfo.GetLogFolder();
            var logPath = Path.Combine(logFolder, "ui-events.log");

            _diskProvider.EnsureFolder(logFolder);

            lock (UiEventsLock)
            {
                using var stream = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(stream);

                foreach (var line in eventsJson)
                {
                    if (line.IsNullOrWhiteSpace())
                    {
                        continue;
                    }

                    writer.WriteLine(line);
                }
            }
        }

        public DiagnosticsPushResult PushDiagnostics()
        {
            if (!IsDevelopBranch())
            {
                return new DiagnosticsPushResult
                {
                    Success = false,
                    Message = "Diagnostics push is only enabled on the develop branch."
                };
            }

            var repo = _configFileProvider.DiagnosticsRepo?.Trim();
            var token = _configFileProvider.DiagnosticsToken?.Trim();

            if (repo.IsNullOrWhiteSpace() || token.IsNullOrWhiteSpace())
            {
                return new DiagnosticsPushResult
                {
                    Success = false,
                    Message = "Diagnostics repo or token is not configured."
                };
            }

            var repoPath = Path.Combine(_appFolderInfo.GetAppDataPath(), "diagnostics-repo");
            var remoteUrl = BuildRemoteUrl(repo, token);
            var sanitizedRemoteUrl = BuildRemoteUrl(repo, string.Empty);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

            try
            {
                EnsureRepo(repoPath, remoteUrl, sanitizedRemoteUrl);
                var branch = GetDefaultBranch(repoPath);

                RunGit(repoPath, $"checkout {branch}");
                RunGit(repoPath, $"pull --rebase origin {branch}");

                var folderName = $"diagnostics/{timestamp}";
                var destinationRoot = Path.Combine(repoPath, "diagnostics", timestamp);
                _diskProvider.EnsureFolder(destinationRoot);

                CopyLogSources(destinationRoot);
                WriteSanitizedConfig(destinationRoot);
                WriteMetadata(destinationRoot, repo, sanitizedRemoteUrl, timestamp);

                RunGit(repoPath, "add .");

                if (!HasPendingChanges(repoPath))
                {
                    return new DiagnosticsPushResult
                    {
                        Success = true,
                        Message = "No diagnostics changes to push.",
                        Folder = folderName
                    };
                }

                RunGit(repoPath, $"commit -m \"Diagnostics {timestamp}\"");
                RunGit(repoPath, $"push origin {branch}");
                RunGit(repoPath, $"remote set-url origin {sanitizedRemoteUrl}");

                var commitHash = GetCurrentCommit(repoPath);

                return new DiagnosticsPushResult
                {
                    Success = true,
                    Message = "Diagnostics pushed.",
                    Commit = commitHash,
                    Folder = folderName
                };
            }
            catch (Exception ex)
            {
                var safeDetails = RedactToken(ex.Message ?? string.Empty);
                if (safeDetails.Length > 500)
                {
                    safeDetails = safeDetails.Substring(0, 500);
                }

                _logger.Error(ex, "Diagnostics push failed.");
                return new DiagnosticsPushResult
                {
                    Success = false,
                    Message = safeDetails.IsNullOrWhiteSpace()
                        ? "Diagnostics push failed. Check server logs for details."
                        : $"Diagnostics push failed: {safeDetails}"
                };
            }
        }

        private bool IsDevelopBranch()
        {
            return _configFileProvider.Branch.Equals("develop", StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureRepo(string repoPath, string remoteUrl, string sanitizedRemoteUrl)
        {
            if (_diskProvider.FolderExists(Path.Combine(repoPath, ".git")))
            {
                RunGit(repoPath, $"remote set-url origin {remoteUrl}");
                return;
            }

            _diskProvider.EnsureFolder(Path.GetDirectoryName(repoPath));
            RunGit(null, $"clone {remoteUrl} \"{repoPath}\"");
            RunGit(repoPath, $"remote set-url origin {sanitizedRemoteUrl}");
        }

        private void CopyLogSources(string destinationRoot)
        {
            var sources = new Dictionary<string, string>
            {
                { "app-logs", _appFolderInfo.GetLogFolder() },
                { "update-logs", _appFolderInfo.GetUpdateLogFolder() },
                { "legacy-logs", Path.Combine(_appFolderInfo.GetAppDataPath(), "Logs") }
            };

            foreach (var source in sources)
            {
                if (!_diskProvider.FolderExists(source.Value))
                {
                    continue;
                }

                var destination = Path.Combine(destinationRoot, source.Key);
                CopyLogFiles(source.Value, destination);
            }
        }

        private void CopyLogFiles(string sourceFolder, string destinationFolder)
        {
            var files = _diskProvider.GetFiles(sourceFolder, true)
                .Where(path => AllowedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(sourceFolder, file);
                var destinationPath = Path.Combine(destinationFolder, relativePath);
                var destinationDir = Path.GetDirectoryName(destinationPath);

                if (!string.IsNullOrWhiteSpace(destinationDir))
                {
                    _diskProvider.EnsureFolder(destinationDir);
                }

                _diskProvider.CopyFile(file, destinationPath, true);
            }
        }

        private void WriteSanitizedConfig(string destinationRoot)
        {
            var sourceConfig = _appFolderInfo.GetConfigPath();
            if (!_diskProvider.FileExists(sourceConfig))
            {
                return;
            }

            var configContents = _diskProvider.ReadAllText(sourceConfig);
            var document = System.Xml.Linq.XDocument.Parse(configContents);

            foreach (var element in document.Descendants())
            {
                if (SensitiveXmlElement.IsMatch(element.Name.LocalName))
                {
                    element.Value = "REDACTED";
                }
            }

            var destinationPath = Path.Combine(destinationRoot, "config.xml");
            _diskProvider.WriteAllText(destinationPath, document.ToString());
        }

        private void WriteMetadata(string destinationRoot, string repo, string remoteUrl, string timestamp)
        {
            var metadata = new
            {
                timestamp,
                version = BuildInfo.Version.ToString(),
                branch = _configFileProvider.Branch,
                instance = _configFileProvider.InstanceName,
                repo,
                remoteUrl,
                appData = _appFolderInfo.GetAppDataPath(),
                logFolder = _appFolderInfo.GetLogFolder()
            };

            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
            _diskProvider.WriteAllText(Path.Combine(destinationRoot, "diagnostics.json"), json);
        }

        private string BuildRemoteUrl(string repo, string token)
        {
            var trimmed = repo.Trim();
            var baseUrl = trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? trimmed
                : $"https://github.com/{trimmed}.git";

            if (token.IsNullOrWhiteSpace())
            {
                return baseUrl;
            }

            var safeToken = Uri.EscapeDataString(token);
            const string tokenUser = "x-access-token";

            if (baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return $"https://{tokenUser}:{safeToken}@{baseUrl.Substring("https://".Length)}";
            }

            if (baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                return $"http://{tokenUser}:{safeToken}@{baseUrl.Substring("http://".Length)}";
            }

            return baseUrl;
        }

        private string GetDefaultBranch(string repoPath)
        {
            var output = RunGit(repoPath, "remote show origin");
            var line = output.Standard.FirstOrDefault(l => l.Content.Contains("HEAD branch:", StringComparison.OrdinalIgnoreCase));

            if (line != null)
            {
                var parts = line.Content.Split(':');
                if (parts.Length > 1)
                {
                    return parts[1].Trim();
                }
            }

            return "main";
        }

        private bool HasPendingChanges(string repoPath)
        {
            var output = RunGit(repoPath, "status --porcelain");
            return output.Standard.Any(line => !line.Content.IsNullOrWhiteSpace());
        }

        private string GetCurrentCommit(string repoPath)
        {
            var output = RunGit(repoPath, "rev-parse HEAD");
            return output.Standard.FirstOrDefault()?.Content?.Trim();
        }

        private ProcessOutput RunGit(string repoPath, string args)
        {
            var commandArgs = repoPath.IsNullOrWhiteSpace() ? args : $"-C \"{repoPath}\" {args}";
            var output = _processProvider.StartAndCapture("git", commandArgs);

            if (output.ExitCode != 0)
            {
                var error = string.Join(Environment.NewLine, output.Error.Select(line => line.Content).Where(line => !line.IsNullOrWhiteSpace()));
                var standard = string.Join(Environment.NewLine, output.Standard.Select(line => line.Content).Where(line => !line.IsNullOrWhiteSpace()));
                var details = string.Join(Environment.NewLine, new[] { error, standard }.Where(line => !line.IsNullOrWhiteSpace()));

                throw new InvalidOperationException($"git {RedactToken(args)} failed: {RedactToken(details)}");
            }

            return output;
        }

        private string RedactToken(string value)
        {
            var token = _configFileProvider.DiagnosticsToken;
            if (token.IsNullOrWhiteSpace())
            {
                return value;
            }

            return value?.Replace(token, "REDACTED");
        }
    }
}
