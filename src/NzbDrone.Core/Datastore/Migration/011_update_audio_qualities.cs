using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using FluentMigrator;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(011)]
    public class update_audio_qualities : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(UpdateAudioQualities);
        }

        private void UpdateAudioQualities(IDbConnection conn, IDbTransaction tran)
        {
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<ProfileItem10>>(new QualityIntConverter()));
            var updater = new ProfileUpdater10(conn, tran);
            updater.SplitQualityAppend(10, 12); // Add M4B above MP3
            updater.SplitQualityPrepend(10, 13); // Add UnknownAudio below MP3
            updater.Commit();
        }

        public class Profile10
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Cutoff { get; set; }
            public List<ProfileItem10> Items { get; set; }
        }

        public class ProfileItem10
        {
            public int Quality { get; set; }
            public bool Allowed { get; set; }
            public List<ProfileItem10> Items { get; set; } = new List<ProfileItem10>();
        }

        public class ProfileUpdater10
        {
            private readonly IDbConnection _connection;
            private readonly IDbTransaction _transaction;

            private List<Profile10> _profiles;
            private HashSet<Profile10> _changedProfiles = new HashSet<Profile10>();

            public ProfileUpdater10(IDbConnection conn, IDbTransaction tran)
            {
                _connection = conn;
                _transaction = tran;

                _profiles = _connection.Query<Profile10>(@"SELECT ""Id"", ""Name"", ""Cutoff"", ""Items"" FROM ""QualityProfiles""",
                    transaction: _transaction).ToList();
            }

            public void Commit()
            {
                var sql = "UPDATE \"QualityProfiles\" SET \"Name\" = @Name, \"Cutoff\" = @Cutoff, \"Items\" = @Items WHERE \"Id\" = @Id";
                _connection.Execute(sql, _changedProfiles, transaction: _transaction);

                _changedProfiles.Clear();
            }

            public void PrependQuality(int quality)
            {
                foreach (var profile in _profiles)
                {
                    if (profile.Items.Any(v => v.Quality == quality))
                    {
                        continue;
                    }

                    profile.Items.Insert(0, new ProfileItem10
                    {
                        Quality = quality,
                        Allowed = false
                    });

                    _changedProfiles.Add(profile);
                }
            }

            public void AppendQuality(int quality)
            {
                foreach (var profile in _profiles)
                {
                    if (profile.Items.Any(v => v.Quality == quality))
                    {
                        continue;
                    }

                    profile.Items.Add(new ProfileItem10
                    {
                        Quality = quality,
                        Allowed = false
                    });

                    _changedProfiles.Add(profile);
                }
            }

            public void SplitQualityPrepend(int find, int quality)
            {
                foreach (var profile in _profiles)
                {
                    if (profile.Items.Any(v => v.Quality == quality))
                    {
                        continue;
                    }

                    var findIndex = profile.Items.FindIndex(v => v.Quality == find);

                    profile.Items.Insert(findIndex, new ProfileItem10
                    {
                        Quality = quality,
                        Allowed = profile.Items[findIndex].Allowed
                    });

                    if (profile.Cutoff == find)
                    {
                        profile.Cutoff = quality;
                    }

                    _changedProfiles.Add(profile);
                }
            }

            public void SplitQualityAppend(int find, int quality)
            {
                foreach (var profile in _profiles)
                {
                    if (profile.Items.Any(v => v.Quality == quality))
                    {
                        continue;
                    }

                    var findIndex = profile.Items.FindIndex(v => v.Quality == find);

                    profile.Items.Insert(findIndex + 1, new ProfileItem10
                    {
                        Quality = quality,
                        Allowed = profile.Items[findIndex].Allowed
                    });

                    _changedProfiles.Add(profile);
                }
            }
        }
    }
}
