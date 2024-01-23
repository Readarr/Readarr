using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(23)]
    public class postgres_update_timestamp_columns_to_with_timezone : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.FromTable("Commands").AllRows();

            Alter.Table("Authors").AlterColumn("LastInfoSync").AsDateTimeOffset().Nullable();
            Alter.Table("Authors").AlterColumn("Added").AsDateTimeOffset().Nullable();
            Alter.Table("AuthorMetadata").AlterColumn("Born").AsDateTimeOffset().Nullable();
            Alter.Table("AuthorMetadata").AlterColumn("Died").AsDateTimeOffset().Nullable();
            Alter.Table("Blocklist").AlterColumn("Date").AsDateTimeOffset().NotNullable();
            Alter.Table("Blocklist").AlterColumn("PublishedDate").AsDateTimeOffset().Nullable();
            Alter.Table("Books").AlterColumn("ReleaseDate").AsDateTimeOffset().Nullable();
            Alter.Table("Books").AlterColumn("LastInfoSync").AsDateTimeOffset().Nullable();
            Alter.Table("Books").AlterColumn("Added").AsDateTimeOffset().Nullable();
            Alter.Table("BookFiles").AlterColumn("DateAdded").AsDateTimeOffset().Nullable();
            Alter.Table("BookFiles").AlterColumn("Modified").AsDateTimeOffset().Nullable();
            Alter.Table("Commands").AlterColumn("QueuedAt").AsDateTimeOffset().NotNullable();
            Alter.Table("Commands").AlterColumn("StartedAt").AsDateTimeOffset().Nullable();
            Alter.Table("Commands").AlterColumn("EndedAt").AsDateTimeOffset().Nullable();
            Alter.Table("DownloadClientStatus").AlterColumn("InitialFailure").AsDateTimeOffset().Nullable();
            Alter.Table("DownloadClientStatus").AlterColumn("MostRecentFailure").AsDateTimeOffset().Nullable();
            Alter.Table("DownloadClientStatus").AlterColumn("DisabledTill").AsDateTimeOffset().Nullable();
            Alter.Table("Editions").AlterColumn("ReleaseDate").AsDateTimeOffset().Nullable();
            Alter.Table("ExtraFiles").AlterColumn("Added").AsDateTimeOffset().NotNullable();
            Alter.Table("ExtraFiles").AlterColumn("LastUpdated").AsDateTimeOffset().NotNullable();
            Alter.Table("History").AlterColumn("Date").AsDateTimeOffset().NotNullable();
            Alter.Table("ImportListStatus").AlterColumn("InitialFailure").AsDateTimeOffset().Nullable();
            Alter.Table("ImportListStatus").AlterColumn("MostRecentFailure").AsDateTimeOffset().Nullable();
            Alter.Table("ImportListStatus").AlterColumn("DisabledTill").AsDateTimeOffset().Nullable();
            Alter.Table("IndexerStatus").AlterColumn("InitialFailure").AsDateTimeOffset().Nullable();
            Alter.Table("IndexerStatus").AlterColumn("MostRecentFailure").AsDateTimeOffset().Nullable();
            Alter.Table("IndexerStatus").AlterColumn("DisabledTill").AsDateTimeOffset().Nullable();
            Alter.Table("MetadataFiles").AlterColumn("LastUpdated").AsDateTimeOffset().NotNullable();
            Alter.Table("MetadataFiles").AlterColumn("Added").AsDateTimeOffset().Nullable();
            Alter.Table("PendingReleases").AlterColumn("Added").AsDateTimeOffset().NotNullable();
            Alter.Table("ScheduledTasks").AlterColumn("LastExecution").AsDateTimeOffset().NotNullable();
            Alter.Table("ScheduledTasks").AlterColumn("LastStartTime").AsDateTimeOffset().Nullable();
            Alter.Table("VersionInfo").AlterColumn("AppliedOn").AsDateTimeOffset().Nullable();
        }

        protected override void LogDbUpgrade()
        {
            Alter.Table("Logs").AlterColumn("Time").AsDateTimeOffset().NotNullable();
            Alter.Table("VersionInfo").AlterColumn("AppliedOn").AsDateTimeOffset().Nullable();
        }

        protected override void CacheDbUpgrade()
        {
            Alter.Table("HttpResponse").AlterColumn("LastRefresh").AsDateTimeOffset().Nullable();
            Alter.Table("HttpResponse").AlterColumn("Expiry").AsDateTimeOffset().Nullable();
        }
    }
}
