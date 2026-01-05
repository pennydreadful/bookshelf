using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Books;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles
{
    public class CombineAudiobookService : IExecute<CombineAudiobookCommand>
    {
        private static readonly Regex FfmpegTimeRegex = new Regex(@"time=(\d{2}:\d{2}:\d{2}\.\d{2})", RegexOptions.Compiled);
        private static readonly TimeSpan DeleteDelay = TimeSpan.FromHours(1);

        private readonly IBookService _bookService;
        private readonly IEditionService _editionService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IDeleteMediaFiles _mediaFileDeletionService;
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IConfigService _configService;
        private readonly IDiskProvider _diskProvider;
        private readonly IProcessProvider _processProvider;
        private readonly Logger _logger;

        public CombineAudiobookService(IBookService bookService,
                                       IEditionService editionService,
                                       IMediaFileService mediaFileService,
                                       IDeleteMediaFiles mediaFileDeletionService,
                                       IBuildFileNames fileNameBuilder,
                                       IConfigService configService,
                                       IDiskProvider diskProvider,
                                       IProcessProvider processProvider,
                                       Logger logger)
        {
            _bookService = bookService;
            _editionService = editionService;
            _mediaFileService = mediaFileService;
            _mediaFileDeletionService = mediaFileDeletionService;
            _fileNameBuilder = fileNameBuilder;
            _configService = configService;
            _diskProvider = diskProvider;
            _processProvider = processProvider;
            _logger = logger;
        }

        public void Execute(CombineAudiobookCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (_configService.CombineAudiobookMode == CombineAudiobookMode.Disabled)
            {
                throw new InvalidOperationException("Combine audiobooks is disabled in settings.");
            }

            if (command.BookFileIds == null || command.BookFileIds.Count < 2)
            {
                throw new InvalidOperationException("At least two audiobook files are required.");
            }

            var bookFiles = _mediaFileService.Get(command.BookFileIds);
            if (bookFiles.Count != command.BookFileIds.Count)
            {
                throw new InvalidOperationException("One or more audiobook files could not be found.");
            }

            var orderedFiles = command.BookFileIds
                .Select(id => bookFiles.Single(f => f.Id == id))
                .ToList();

            ValidateFiles(orderedFiles);

            var editionId = orderedFiles.Select(f => f.EditionId).Distinct().Single();
            var edition = _editionService.GetEdition(editionId);
            var book = _bookService.GetBook(command.BookId);
            var author = book.Author.Value;

            NormalizePartFiles(book, orderedFiles);

            var partInfos = BuildPartInfos(orderedFiles);
            var outputPath = BuildOutputPath(author, edition, orderedFiles.First());

            EnsureOutputAvailable(outputPath);

            var workFolder = _diskProvider.GetParentFolder(orderedFiles.First().Path);
            var token = Guid.NewGuid().ToString("N");
            var concatListPath = Path.Combine(workFolder, $".bookdarr-concat-{token}.txt");
            var metadataPath = Path.Combine(workFolder, $".bookdarr-metadata-{token}.txt");
            var includeChapters = _configService.CombineAudiobookChapters && partInfos.All(p => p.Duration > TimeSpan.Zero);

            try
            {
                _diskProvider.WriteAllText(concatListPath, BuildConcatList(partInfos));

                if (includeChapters)
                {
                    _diskProvider.WriteAllText(metadataPath, BuildChapterMetadata(partInfos));
                }

                RunFfmpeg(concatListPath, metadataPath, includeChapters, outputPath, partInfos);
            }
            finally
            {
                SafeDelete(concatListPath);
                if (includeChapters)
                {
                    SafeDelete(metadataPath);
                }
            }

            var outputFile = CreateOutputBookFile(author, edition, outputPath);
            _mediaFileService.Add(outputFile);

            HandleSourceCleanup(orderedFiles);
        }

        private void ValidateFiles(List<BookFile> orderedFiles)
        {
            if (orderedFiles.Select(f => f.EditionId).Distinct().Count() != 1)
            {
                throw new InvalidOperationException("All audiobook files must be from the same edition.");
            }

            if (orderedFiles.Any(f => !IsMp3(f.Path)))
            {
                throw new InvalidOperationException("Only MP3 audiobook files can be combined.");
            }
        }

        private void NormalizePartFiles(Book book, List<BookFile> orderedFiles)
        {
            var padding = orderedFiles.Count.ToString(CultureInfo.InvariantCulture).Length;
            var cleanTitle = FileNameBuilder.CleanFileName(book.Title);

            for (var i = 0; i < orderedFiles.Count; i++)
            {
                var file = orderedFiles[i];
                var partNumber = (i + 1).ToString(CultureInfo.InvariantCulture).PadLeft(padding, '0');
                var extension = Path.GetExtension(file.Path);
                var folder = _diskProvider.GetParentFolder(file.Path);
                var newFileName = $"{cleanTitle}-{partNumber}{extension}";
                var newPath = Path.Combine(folder, newFileName);

                if (!file.Path.PathEquals(newPath))
                {
                    _diskProvider.MoveFile(file.Path, newPath, true);
                    file.Path = newPath;
                }

                var fileInfo = _diskProvider.GetFileInfo(file.Path);
                file.Part = i + 1;
                file.PartCount = orderedFiles.Count;
                file.Size = fileInfo.Length;
                file.Modified = fileInfo.LastWriteTimeUtc;
            }

            _mediaFileService.Update(orderedFiles);
        }

        private List<PartInfo> BuildPartInfos(List<BookFile> orderedFiles)
        {
            var parts = new List<PartInfo>(orderedFiles.Count);
            double currentStart = 0;

            foreach (var file in orderedFiles)
            {
                var tag = new AudioTag(file.Path);
                var duration = tag.IsValid ? tag.Duration : TimeSpan.Zero;
                var bitrate = tag.MediaInfo?.AudioBitrate ?? 0;
                var name = Path.GetFileNameWithoutExtension(file.Path);
                var partInfo = new PartInfo
                {
                    BookFile = file,
                    Duration = duration,
                    Bitrate = bitrate,
                    Name = name,
                    StartSeconds = currentStart
                };

                currentStart += duration.TotalSeconds;
                partInfo.EndSeconds = currentStart;
                parts.Add(partInfo);
            }

            return parts;
        }

        private string BuildOutputPath(Author author, Edition edition, BookFile referenceFile)
        {
            var mode = _configService.CombineAudiobookMode;
            var outputQuality = mode == CombineAudiobookMode.Mp3ToM4b ? Quality.M4B : Quality.MP3;
            var namingBookFile = new BookFile
            {
                Part = 1,
                PartCount = 1,
                Quality = new QualityModel(outputQuality),
                MediaInfo = new MediaInfoModel()
            };

            var fileName = _fileNameBuilder.BuildBookFileName(author, edition, namingBookFile);
            var extension = mode == CombineAudiobookMode.Mp3ToM4b ? ".m4b" : ".mp3";

            return _fileNameBuilder.BuildBookFilePath(author, edition, fileName, extension);
        }

        private void EnsureOutputAvailable(string outputPath)
        {
            if (!_diskProvider.FileExists(outputPath))
            {
                return;
            }

            var existingFile = _mediaFileService.GetFileWithPath(outputPath);
            if (existingFile != null)
            {
                _mediaFileDeletionService.DeleteTrackFile(existingFile);
                return;
            }

            _diskProvider.DeleteFile(outputPath);
        }

        private void RunFfmpeg(string concatListPath, string metadataPath, bool includeChapters, string outputPath, List<PartInfo> partInfos)
        {
            var totalSeconds = partInfos.Sum(p => p.Duration.TotalSeconds);
            var targetBitrate = GetTargetBitrate(partInfos, totalSeconds);
            var args = BuildFfmpegArgs(concatListPath, metadataPath, includeChapters, outputPath, targetBitrate);
            var lastPercent = -1;

            Action<string> progressHandler = (line) =>
            {
                var timeSeconds = ParseFfmpegTimeSeconds(line);
                if (!timeSeconds.HasValue || totalSeconds <= 0)
                {
                    return;
                }

                var percent = (int)Math.Min(100, Math.Floor(timeSeconds.Value / totalSeconds * 100));
                if (percent == lastPercent)
                {
                    return;
                }

                var currentPart = GetCurrentPartName(partInfos, timeSeconds.Value);
                _logger.ProgressInfo("CombineAudiobookProgress|{0}|{1}", percent, currentPart);
                lastPercent = percent;
            };

            _logger.ProgressInfo("Combining audiobook files...");

            var process = _processProvider.Start("ffmpeg", args, null, null, progressHandler);
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("Failed to combine audiobook files.");
            }
        }

        private string BuildFfmpegArgs(string concatListPath, string metadataPath, bool includeChapters, string outputPath, int targetBitrate)
        {
            var mode = _configService.CombineAudiobookMode;
            var builder = new StringBuilder();

            builder.Append("-hide_banner -nostdin -y ");
            builder.Append("-f concat -safe 0 ");
            builder.Append($"-i \"{concatListPath}\" ");

            if (includeChapters)
            {
                builder.Append($"-i \"{metadataPath}\" ");
                builder.Append("-map_metadata 1 -map_chapters 1 ");
            }

            builder.Append("-map 0:a ");

            if (mode == CombineAudiobookMode.Mp3ToMp3)
            {
                builder.Append("-c copy ");
            }
            else
            {
                builder.Append($"-c:a aac -b:a {targetBitrate}k ");
            }

            builder.Append($"\"{outputPath}\"");

            return builder.ToString();
        }

        private BookFile CreateOutputBookFile(Author author, Edition edition, string outputPath)
        {
            var outputTag = new AudioTag(outputPath);
            var outputInfo = _diskProvider.GetFileInfo(outputPath);
            var outputQuality = outputTag.IsValid ? outputTag.Quality : new QualityModel(MediaFileExtensions.GetQualityForExtension(Path.GetExtension(outputPath)));
            var mediaInfo = outputTag.IsValid ? outputTag.MediaInfo : new MediaInfoModel();

            return new BookFile
            {
                Path = outputPath,
                Size = outputInfo.Length,
                Modified = outputInfo.LastWriteTimeUtc,
                DateAdded = DateTime.UtcNow,
                Quality = outputQuality,
                MediaInfo = mediaInfo,
                MediaType = BookFileMediaType.Audiobook,
                Part = 1,
                PartCount = 1,
                EditionId = edition.Id,
                Author = author,
                Edition = edition
            };
        }

        private void HandleSourceCleanup(List<BookFile> orderedFiles)
        {
            switch (_configService.CombineAudiobookDeleteMode)
            {
                case CombineAudiobookDeleteMode.DeleteImmediately:
                    DeleteSourceFiles(orderedFiles);
                    break;
                case CombineAudiobookDeleteMode.DeleteAfterOneHour:
                    Task.Run(async () =>
                    {
                        await Task.Delay(DeleteDelay).ConfigureAwait(false);
                        DeleteSourceFiles(orderedFiles);
                    });
                    break;
                case CombineAudiobookDeleteMode.Keep:
                    break;
            }
        }

        private void DeleteSourceFiles(List<BookFile> orderedFiles)
        {
            foreach (var file in orderedFiles)
            {
                try
                {
                    _mediaFileDeletionService.DeleteTrackFile(file);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to delete source audiobook file {0}", file.Path);
                }
            }
        }

        private string BuildConcatList(List<PartInfo> partInfos)
        {
            var lines = partInfos.Select(p => $"file '{EscapeConcatPath(p.BookFile.Path)}'");
            return string.Join(Environment.NewLine, lines);
        }

        private string BuildChapterMetadata(List<PartInfo> partInfos)
        {
            var builder = new StringBuilder();
            builder.AppendLine(";FFMETADATA1");

            foreach (var part in partInfos)
            {
                var start = (long)Math.Round(part.StartSeconds * 1000);
                var end = (long)Math.Round(part.EndSeconds * 1000);

                builder.AppendLine("[CHAPTER]");
                builder.AppendLine("TIMEBASE=1/1000");
                builder.AppendLine($"START={start}");
                builder.AppendLine($"END={end}");
                builder.AppendLine($"title={part.Name}");
            }

            return builder.ToString();
        }

        private int GetTargetBitrate(List<PartInfo> partInfos, double totalSeconds)
        {
            var weightedBitrate = partInfos
                .Where(p => p.Bitrate > 0 && p.Duration > TimeSpan.Zero)
                .Sum(p => p.Bitrate * p.Duration.TotalSeconds);

            if (weightedBitrate > 0 && totalSeconds > 0)
            {
                return (int)Math.Round(weightedBitrate / totalSeconds);
            }

            return 128;
        }

        private double? ParseFfmpegTimeSeconds(string line)
        {
            var match = FfmpegTimeRegex.Match(line);
            if (!match.Success)
            {
                return null;
            }

            if (TimeSpan.TryParseExact(match.Groups[1].Value, "hh\\:mm\\:ss\\.ff", CultureInfo.InvariantCulture, out var time))
            {
                return time.TotalSeconds;
            }

            return null;
        }

        private string GetCurrentPartName(List<PartInfo> partInfos, double timeSeconds)
        {
            var current = partInfos.FirstOrDefault(p => timeSeconds < p.EndSeconds);
            return current?.Name ?? partInfos.Last().Name;
        }

        private bool IsMp3(string path)
        {
            return string.Equals(Path.GetExtension(path), ".mp3", StringComparison.OrdinalIgnoreCase);
        }

        private string EscapeConcatPath(string path)
        {
            return path.Replace("'", "'\\''");
        }

        private void SafeDelete(string path)
        {
            if (path.IsNullOrWhiteSpace())
            {
                return;
            }

            try
            {
                if (_diskProvider.FileExists(path))
                {
                    _diskProvider.DeleteFile(path);
                }
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Unable to delete temporary file {0}", path);
            }
        }

        private class PartInfo
        {
            public BookFile BookFile { get; set; }
            public TimeSpan Duration { get; set; }
            public int Bitrate { get; set; }
            public string Name { get; set; }
            public double StartSeconds { get; set; }
            public double EndSeconds { get; set; }
        }
    }
}
