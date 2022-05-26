using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(22)]
    public class EditionMonitoredIndex : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.Index().OnTable("Editions").OnColumn("Monitored");
        }
    }
}
