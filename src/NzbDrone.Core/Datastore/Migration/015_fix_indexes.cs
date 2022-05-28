using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(15)]
    public class FixIndexes : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            IfDatabase("sqlite").Delete.Index().OnTable("Books").OnColumn("AuthorId");
            IfDatabase("sqlite").Delete.Index().OnTable("Books").OnColumns("AuthorId", "ReleaseDate");

            Create.Index().OnTable("Editions").OnColumn("BookId");
        }
    }
}
