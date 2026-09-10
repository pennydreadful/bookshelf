using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(041)]
    public class bookfile_last_remote_search : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("BookFiles").AddColumn("LastRemoteSearchTime").AsDateTimeOffset().Nullable();
        }
    }
}
