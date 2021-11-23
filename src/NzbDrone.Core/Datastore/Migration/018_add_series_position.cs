using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(18)]
    public class AddSeriesPosition : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("SeriesBookLink").AddColumn("SeriesPosition").AsInt32().WithDefaultValue(0);
        }
    }
}
