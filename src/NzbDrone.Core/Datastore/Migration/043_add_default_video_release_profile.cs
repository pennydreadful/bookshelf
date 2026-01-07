using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using FluentMigrator;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(043)]
    public class add_default_video_release_profile : NzbDroneMigrationBase
    {
        private static readonly List<string> VideoIgnoredTerms = new List<string>
        {
            "1080p",
            "720p",
            "2160p",
            "480p",
            "webrip",
            "web dl",
            "web-dl",
            "webdl",
            "bluray",
            "blu-ray",
            "bdrip",
            "hdrip",
            "dvdrip",
            "hdtv",
            "x264",
            "x265",
            "h264",
            "h265",
            "h.264",
            "h.265",
            "hevc",
            "av1",
            "remux"
        };

        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(AddVideoReleaseProfile);
        }

        private void AddVideoReleaseProfile(IDbConnection conn, IDbTransaction tran)
        {
            var profiles = conn.Query<ReleaseProfile043>(
                @"SELECT ""Id"", ""Required"", ""Ignored"" FROM ""ReleaseProfiles""",
                transaction: tran).ToList();

            if (profiles.Any(profile => HasVideoTerms(profile.Ignored)))
            {
                return;
            }

            var profileRow = new
            {
                Required = new List<string>().ToJson(),
                Ignored = VideoIgnoredTerms.ToJson(),
                Tags = new HashSet<int>().ToJson(),
                Enabled = true,
                IndexerId = 0
            };

            conn.Execute(@"INSERT INTO ""ReleaseProfiles"" (""Required"", ""Ignored"", ""Tags"", ""Enabled"", ""IndexerId"")
                          VALUES (@Required, @Ignored, @Tags, @Enabled, @IndexerId)",
                profileRow,
                transaction: tran);
        }

        private static bool HasVideoTerms(string rawIgnored)
        {
            if (string.IsNullOrWhiteSpace(rawIgnored))
            {
                return false;
            }

            var ignored = DeserializeTerms(rawIgnored);

            if (ignored.Count == 0)
            {
                return false;
            }

            var matches = ignored.Intersect(VideoIgnoredTerms, StringComparer.OrdinalIgnoreCase).Count();
            return matches >= 3;
        }

        private static List<string> DeserializeTerms(string raw)
        {
            try
            {
                var parsed = Json.Deserialize<List<string>>(raw);
                return parsed ?? new List<string>();
            }
            catch
            {
                return raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(term => term.Trim())
                    .Where(term => term.Length > 0)
                    .ToList();
            }
        }

        private class ReleaseProfile043
        {
            public int Id { get; set; }
            public string Required { get; set; }
            public string Ignored { get; set; }
        }
    }
}
