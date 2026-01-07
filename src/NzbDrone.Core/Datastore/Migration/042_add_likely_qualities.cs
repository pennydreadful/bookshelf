using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using FluentMigrator;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(042)]
    public class add_likely_qualities : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(UpdateLikelyQualities);
        }

        private void UpdateLikelyQualities(IDbConnection conn, IDbTransaction tran)
        {
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<ProfileItem42>>(new QualityIntConverter()));
            var updater = new ProfileUpdater42(conn, tran);
            updater.SplitQualityAppend(Quality.Unknown.Id, Quality.LikelyEbook.Id);
            updater.SplitQualityAppend(Quality.UnknownAudio.Id, Quality.LikelyAudiobook.Id);
            updater.Commit();
        }

        public class Profile42
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Cutoff { get; set; }
            public List<ProfileItem42> Items { get; set; }
        }

        public class ProfileItem42
        {
            public int Quality { get; set; }
            public bool Allowed { get; set; }
            public List<ProfileItem42> Items { get; set; } = new List<ProfileItem42>();
        }

        public class ProfileUpdater42
        {
            private readonly IDbConnection _connection;
            private readonly IDbTransaction _transaction;

            private List<Profile42> _profiles;
            private HashSet<Profile42> _changedProfiles = new HashSet<Profile42>();

            public ProfileUpdater42(IDbConnection conn, IDbTransaction tran)
            {
                _connection = conn;
                _transaction = tran;

                _profiles = _connection.Query<Profile42>(@"SELECT ""Id"", ""Name"", ""Cutoff"", ""Items"" FROM ""QualityProfiles""",
                    transaction: _transaction).ToList();
            }

            public void Commit()
            {
                var sql = "UPDATE \"QualityProfiles\" SET \"Name\" = @Name, \"Cutoff\" = @Cutoff, \"Items\" = @Items WHERE \"Id\" = @Id";
                _connection.Execute(sql, _changedProfiles, transaction: _transaction);

                _changedProfiles.Clear();
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

                    if (findIndex < 0)
                    {
                        continue;
                    }

                    profile.Items.Insert(findIndex + 1, new ProfileItem42
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
