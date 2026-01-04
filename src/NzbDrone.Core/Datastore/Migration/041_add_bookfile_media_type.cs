using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(041)]
    public class add_bookfile_media_type : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("BookFiles").AddColumn("MediaType").AsInt32().WithDefaultValue(0);

            Execute.Sql(@"UPDATE ""BookFiles""
                          SET ""MediaType"" = 1
                          WHERE lower(""Path"") LIKE '%.epub'
                             OR lower(""Path"") LIKE '%.kepub'
                             OR lower(""Path"") LIKE '%.mobi'
                             OR lower(""Path"") LIKE '%.azw3'
                             OR lower(""Path"") LIKE '%.pdf'");

            Execute.Sql(@"UPDATE ""BookFiles""
                          SET ""MediaType"" = 2
                          WHERE lower(""Path"") LIKE '%.flac'
                             OR lower(""Path"") LIKE '%.ape'
                             OR lower(""Path"") LIKE '%.wavpack'
                             OR lower(""Path"") LIKE '%.wav'
                             OR lower(""Path"") LIKE '%.alac'
                             OR lower(""Path"") LIKE '%.mp2'
                             OR lower(""Path"") LIKE '%.mp3'
                             OR lower(""Path"") LIKE '%.wma'
                             OR lower(""Path"") LIKE '%.m4a'
                             OR lower(""Path"") LIKE '%.m4p'
                             OR lower(""Path"") LIKE '%.m4b'
                             OR lower(""Path"") LIKE '%.aac'
                             OR lower(""Path"") LIKE '%.mp4a'
                             OR lower(""Path"") LIKE '%.ogg'
                             OR lower(""Path"") LIKE '%.oga'
                             OR lower(""Path"") LIKE '%.vorbis'");
        }
    }
}
