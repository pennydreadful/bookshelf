using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles
{
    public static class MediaFileExtensions
    {
        private static readonly Dictionary<string, Quality> _textExtensions;
        private static readonly Dictionary<string, Quality> _audioExtensions;

        static MediaFileExtensions()
        {
            _textExtensions = new Dictionary<string, Quality>(StringComparer.OrdinalIgnoreCase)
            {
                { ".epub", Quality.EPUB },
                { ".kepub", Quality.EPUB },
                { ".mobi", Quality.MOBI },
                { ".azw3", Quality.AZW3 },
                { ".pdf", Quality.PDF },
            };

            _audioExtensions = new Dictionary<string, Quality>(StringComparer.OrdinalIgnoreCase)
            {
                { ".flac", Quality.FLAC },
                { ".ape", Quality.FLAC },
                { ".wavpack", Quality.FLAC },
                { ".wav", Quality.FLAC },
                { ".alac", Quality.FLAC },
                { ".mp2", Quality.MP3 },
                { ".mp3", Quality.MP3 },
                { ".wma", Quality.MP3 },
                { ".m4a", Quality.MP3 },
                { ".m4p", Quality.MP3 },
                { ".m4b", Quality.M4B },
                { ".aac", Quality.MP3 },
                { ".mp4a", Quality.MP3 },
                { ".ogg", Quality.MP3 },
                { ".oga", Quality.MP3 },
                { ".vorbis", Quality.MP3 },
            };
        }

        public static HashSet<string> TextExtensions => new HashSet<string>(_textExtensions.Keys, StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> AudioExtensions => new HashSet<string>(_audioExtensions.Keys, StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> AllExtensions => new HashSet<string>(_textExtensions.Keys.Concat(_audioExtensions.Keys), StringComparer.OrdinalIgnoreCase);

        public static BookFileMediaType GetMediaTypeForExtension(string extension)
        {
            if (_textExtensions.ContainsKey(extension))
            {
                return BookFileMediaType.Ebook;
            }

            if (_audioExtensions.ContainsKey(extension))
            {
                return BookFileMediaType.Audiobook;
            }

            return BookFileMediaType.Unknown;
        }

        public static BookFileMediaType GetMediaTypeForPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return BookFileMediaType.Unknown;
            }

            return GetMediaTypeForExtension(Path.GetExtension(path));
        }

        public static BookFileMediaType GetMediaTypeForQuality(Quality quality)
        {
            if (quality == null)
            {
                return BookFileMediaType.Unknown;
            }

            if (quality == Quality.MP3 || quality == Quality.M4B || quality == Quality.FLAC || quality == Quality.UnknownAudio || quality == Quality.LikelyAudiobook)
            {
                return BookFileMediaType.Audiobook;
            }

            if (quality == Quality.PDF || quality == Quality.MOBI || quality == Quality.EPUB || quality == Quality.AZW3 || quality == Quality.Unknown || quality == Quality.LikelyEbook)
            {
                return BookFileMediaType.Ebook;
            }

            return BookFileMediaType.Unknown;
        }

        public static Quality GetQualityForExtension(string extension)
        {
            if (_textExtensions.ContainsKey(extension))
            {
                return _textExtensions[extension];
            }

            if (_audioExtensions.ContainsKey(extension))
            {
                return _audioExtensions[extension];
            }

            return Quality.Unknown;
        }
    }
}
