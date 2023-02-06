using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(027)]
    public class remove_omg : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("DELETE FROM Indexers WHERE Implementation = 'Omgwtfnzbs'");
            Execute.Sql("DELETE FROM Indexers WHERE Implementation = 'Rarbg'");
        }
    }
}
