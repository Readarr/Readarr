using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(040)]
    public class add_indexer_flags : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Blocklist").AddColumn("IndexerFlags").AsInt32().WithDefaultValue(0);
            Alter.Table("BookFiles").AddColumn("IndexerFlags").AsInt32().WithDefaultValue(0);
        }
    }
}
