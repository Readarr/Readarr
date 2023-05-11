using System.Data;
using Dapper;
using FluentMigrator;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(013)]
    public class update_author_sort_name_again : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("AuthorMetadata").AddColumn("NameLastFirst").AsString().Nullable();
            Alter.Table("AuthorMetadata").AddColumn("SortNameLastFirst").AsString().Nullable();
            Execute.WithConnection(MigrateAuthorSortName);
            Alter.Table("AuthorMetadata").AlterColumn("NameLastFirst").AsString().NotNullable();
            Alter.Table("AuthorMetadata").AlterColumn("SortNameLastFirst").AsString().NotNullable();
        }

        private void MigrateAuthorSortName(IDbConnection conn, IDbTransaction tran)
        {
            var rows = conn.Query<AuthorName>("SELECT \"AuthorMetadata\".\"Id\", \"AuthorMetadata\".\"Name\" FROM \"AuthorMetadata\"", transaction: tran);

            foreach (var row in rows)
            {
                row.NameLastFirst = row.Name.ToLastFirst();
                row.SortName = row.Name.ToLower();
                row.SortNameLastFirst = row.Name.ToLastFirst().ToLower();
            }

            var sql = "UPDATE \"AuthorMetadata\" SET \"NameLastFirst\" = @NameLastFirst, \"SortName\" = @SortName, \"SortNameLastFirst\" = @SortNameLastFirst WHERE \"Id\" = @Id";
            conn.Execute(sql, rows, transaction: tran);
        }

        private class AuthorName : ModelBase
        {
            public string Name { get; set; }
            public string NameLastFirst { get; set; }
            public string SortName { get; set; }
            public string SortNameLastFirst { get; set; }
        }
    }
}
