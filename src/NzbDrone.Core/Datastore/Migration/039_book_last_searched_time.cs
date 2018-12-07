using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(039)]
    public class book_last_searched_time : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Books").AddColumn("LastSearchTime").AsDateTimeOffset().Nullable();
        }
    }
}
