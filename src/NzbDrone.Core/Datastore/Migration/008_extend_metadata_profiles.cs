using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(008)]
    public class extend_metadata_profiles : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("MetadataProfiles").AddColumn("MinPages").AsInt32().NotNullable().WithDefaultValue(0);
            Alter.Table("MetadataProfiles").AddColumn("Ignored").AsString().Nullable();
        }
    }
}
