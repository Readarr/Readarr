using System;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.History;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore
{
    [TestFixture]
    public class DatabaseRelationshipFixture : DbTest
    {
        [Test]
        public void one_to_one()
        {
            var book = Builder<Book>.CreateNew()
                .With(c => c.Id = 0)
                .With(x => x.ReleaseDate = DateTime.UtcNow)
                .With(x => x.LastInfoSync = DateTime.UtcNow)
                .With(x => x.Added = DateTime.UtcNow)
                .BuildNew();
            Db.Insert(book);
        }

        [Test]
        public void embedded_document_as_json()
        {
            var quality = new QualityModel { Quality = Quality.MP3, Revision = new Revision(version: 2) };

            var history = Builder<EntityHistory>.CreateNew()
                            .With(c => c.Id = 0)
                            .With(c => c.Quality = quality)
                            .Build();

            Db.Insert(history);

            var loadedQuality = Db.Single<EntityHistory>().Quality;
            loadedQuality.Should().Be(quality);
        }

        [Test]
        public void embedded_list_of_document_with_json()
        {
            var history = Builder<EntityHistory>.CreateListOfSize(2)
                            .All().With(c => c.Id = 0)
                            .Build().ToList();

            history[0].Quality = new QualityModel(Quality.MP3, new Revision(version: 2));
            history[1].Quality = new QualityModel(Quality.MP3, new Revision(version: 2));

            Db.InsertMany(history);

            var returnedHistory = Db.All<EntityHistory>();

            returnedHistory[0].Quality.Quality.Should().Be(Quality.MP3);
        }
    }
}
