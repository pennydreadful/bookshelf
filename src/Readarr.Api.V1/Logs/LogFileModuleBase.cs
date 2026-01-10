using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;

namespace Readarr.Api.V1.Logs
{
    public abstract class LogFileControllerBase : Controller
    {
        protected const string LOGFILE_ROUTE = @"/(?<filename>[-.a-zA-Z0-9]+?\.txt)";
        private const int MaxLogFiles = 10;
        protected string _resource;

        private readonly IDiskProvider _diskProvider;
        private readonly IConfigFileProvider _configFileProvider;

        public LogFileControllerBase(IDiskProvider diskProvider,
                                 IConfigFileProvider configFileProvider,
                                 string resource)
        {
            _diskProvider = diskProvider;
            _configFileProvider = configFileProvider;
            _resource = resource;
        }

        [HttpGet]
        public List<LogFileResource> GetLogFilesResponse()
        {
            var result = new List<LogFileResource>();

            var files = GetLogFiles().ToList();
            var orderedFiles = files.Select(file => new
                {
                    File = file,
                    LastWriteTime = _diskProvider.FileGetLastWrite(file)
                })
                .OrderByDescending(item => item.LastWriteTime)
                .ToList();

            foreach (var staleFile in orderedFiles.Skip(MaxLogFiles))
            {
                try
                {
                    _diskProvider.DeleteFile(staleFile.File);
                }
                catch
                {
                }
            }

            var keptFiles = orderedFiles.Take(MaxLogFiles).ToList();

            for (var i = 0; i < keptFiles.Count; i++)
            {
                var file = keptFiles[i].File;
                var filename = Path.GetFileName(file);

                result.Add(new LogFileResource
                {
                    Id = i + 1,
                    Filename = filename,
                    LastWriteTime = keptFiles[i].LastWriteTime,
                    ContentsUrl = string.Format("{0}/api/v1/{1}/{2}", _configFileProvider.UrlBase, _resource, filename),
                    DownloadUrl = string.Format("{0}/{1}/{2}", _configFileProvider.UrlBase, DownloadUrlRoot, filename)
                });
            }

            return result.OrderByDescending(l => l.LastWriteTime).ToList();
        }

        [HttpGet(@"{filename:regex([[-.a-zA-Z0-9]]+?\.txt)}")]
        public IActionResult GetLogFileResponse(string filename)
        {
            LogManager.Flush();

            var filePath = GetLogFilePath(filename);

            if (!_diskProvider.FileExists(filePath))
            {
                return NotFound();
            }

            return PhysicalFile(filePath, "text/plain");
        }

        protected abstract IEnumerable<string> GetLogFiles();
        protected abstract string GetLogFilePath(string filename);

        protected abstract string DownloadUrlRoot { get; }
    }
}
