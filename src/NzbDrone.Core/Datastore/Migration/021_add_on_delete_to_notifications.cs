using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(21)]
    public class add_on_delete_to_notifications : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Notifications").AddColumn("OnAuthorDelete").AsBoolean().WithDefaultValue(0);
            Alter.Table("Notifications").AddColumn("OnBookDelete").AsBoolean().WithDefaultValue(0);
            Alter.Table("Notifications").AddColumn("OnBookFileDelete").AsBoolean().WithDefaultValue(0);
            Alter.Table("Notifications").AddColumn("OnBookFileDeleteForUpgrade").AsBoolean().WithDefaultValue(0);
        }
    }
}
