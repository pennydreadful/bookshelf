using System;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles
{
    public class BookFile : ModelBase
    {
        // these are model properties
        public string Path { get; set; }
        public long Size { get; set; }
        public DateTime Modified { get; set; }
        public DateTime DateAdded { get; set; }
        public string OriginalFilePath { get; set; }
        public string SceneName { get; set; }
        public string ReleaseGroup { get; set; }
        public QualityModel Quality { get; set; }
        public IndexerFlags IndexerFlags { get; set; }
        public MediaInfoModel MediaInfo { get; set; }
        public int EditionId { get; set; }
        public int CalibreId { get; set; }
        public int Part { get; set; }

        // Set when a remote metadata search was performed successfully but the file could not be
        // identified. Used to avoid re-searching upstream metadata for the same unmatched file on
        // every scan. Reset when the file changes (see MediaFileService.FilterUnchangedFiles).
        public DateTime? LastRemoteSearchTime { get; set; }

        // These are queried from the database
        public LazyLoaded<Author> Author { get; set; }
        public LazyLoaded<Edition> Edition { get; set; }

        // Calculated manually
        public int PartCount { get; set; }

        public override string ToString()
        {
            return string.Format("[{0}] {1}", Id, Path);
        }

        public string GetSceneOrFileName()
        {
            if (SceneName.IsNotNullOrWhiteSpace())
            {
                return SceneName;
            }

            if (Path.IsNotNullOrWhiteSpace())
            {
                return System.IO.Path.GetFileNameWithoutExtension(Path);
            }

            return string.Empty;
        }
    }
}
