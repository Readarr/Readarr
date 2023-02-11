using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(027)]
    public class remove_omg : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.FromTable("Indexers").Row(new { Implementation = "Omgwtfnzbs" });
            Delete.FromTable("Indexers").Row(new { Implementation = "Rarbg" });
        }
    }
}
