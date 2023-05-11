using System.Data;
using Dapper;
using FluentMigrator;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(009)]
    public class update_author_sort_name : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("AuthorMetadata").AddColumn("SortName").AsString().Nullable();
            Execute.WithConnection(MigrateAuthorSortName);
            Alter.Table("AuthorMetadata").AlterColumn("SortName").AsString().NotNullable();

            Delete.Column("SortName").FromTable("Authors");
        }

        private void MigrateAuthorSortName(IDbConnection conn, IDbTransaction tran)
        {
            var rows = conn.Query<AuthorName>("SELECT \"AuthorMetadata\".\"Id\", \"AuthorMetadata\".\"Name\" FROM \"AuthorMetadata\"", transaction: tran);

            foreach (var row in rows)
            {
                row.SortName = row.Name.ToLastFirst().ToLower();
            }

            var sql = "UPDATE \"AuthorMetadata\" SET \"SortName\" = @SortName WHERE \"Id\" = @Id";
            conn.Execute(sql, rows, transaction: tran);
        }

        private class AuthorName : ModelBase
        {
            public string Name { get; set; }
            public string SortName { get; set; }
        }
    }
}
