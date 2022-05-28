using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(012)]
    public class add_bookfile_part_naming_token : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("UPDATE \"NamingConfig\" SET \"StandardBookFormat\" = \"StandardBookFormat\" || '{ (PartNumber)}'");
        }
    }
}
