using System;
using System.Text.RegularExpressions;

namespace NzbDrone.Core.Datastore
{
    public static class DatabaseVersionParser
    {
        // Postgres builds append their own text after the version number, e.g.
        // "16.3 (Debian 16.3-1.pgdg110+1)" or "17.10 - Percona Server for PostgreSQL 17.10.1",
        // which System.Version cannot parse. The version is always the first token.
        private static readonly Regex VersionRegex = new Regex(@"^[^ ]+", RegexOptions.Compiled);

        public static Version ParseServerVersion(string serverVersion)
        {
            var match = VersionRegex.Match(serverVersion);

            return match.Success ? new Version(match.Value) : null;
        }
    }
}
