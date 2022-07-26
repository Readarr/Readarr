using System.Collections.Generic;
using System.Linq;
using Dapper;
using FizzWare.NBuilder;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore
{
    [TestFixture]
    public class LazyLoadingFixture : DbTest
    {
        [SetUp]
        public void Setup()
        {
            SqlBuilderExtensions.LogSql = true;

            var profile = new QualityProfile
            {
                Name = "Test",
                Cutoff = Quality.MP3.Id,
                Items = Qualities.QualityFixture.GetDefaultQualities()
            };

            profile = Db.Insert(profile);

            var metadata = Builder<AuthorMetadata>.CreateNew()
                .With(v => v.Id = 0)
                .Build();
            Db.Insert(metadata);

            var author = Builder<Author>.CreateListOfSize(1)
                .All()
                .With(v => v.Id = 0)
                .With(v => v.QualityProfileId = profile.Id)
                .With(v => v.AuthorMetadataId = metadata.Id)
                .BuildListOfNew();

            Db.InsertMany(author);

            var books = Builder<Book>.CreateListOfSize(3)
                .All()
                .With(v => v.Id = 0)
                .With(v => v.AuthorMetadataId = metadata.Id)
                .BuildListOfNew();

            Db.InsertMany(books);

            var editions = new List<Edition>();
            foreach (var book in books)
            {
                editions.Add(
                    Builder<Edition>.CreateNew()
                    .With(v => v.Id = 0)
                    .With(v => v.BookId = book.Id)
                    .With(v => v.ForeignEditionId = "test" + book.Id)
                    .Build());
            }

            Db.InsertMany(editions);

            var trackFiles = Builder<BookFile>.CreateListOfSize(1)
                .All()
                .With(v => v.Id = 0)
                .With(v => v.EditionId = editions[0].Id)
                .With(v => v.Quality = new QualityModel())
                .BuildListOfNew();

            Db.InsertMany(trackFiles);
        }

        [Test]
        public void should_lazy_load_author_for_trackfile()
        {
            var db = Mocker.Resolve<IDatabase>();
            var tracks = db.Query<BookFile>(new SqlBuilder(db.DatabaseType)).ToList();

            Assert.IsNotEmpty(tracks);
            foreach (var track in tracks)
            {
                Assert.IsFalse(track.Author.IsLoaded);
                Assert.IsNotNull(track.Author.Value);
                Assert.IsTrue(track.Author.IsLoaded);
                Assert.IsTrue(track.Author.Value.Metadata.IsLoaded);
            }
        }

        [Test]
        public void should_lazy_load_trackfile_if_not_joined()
        {
            var db = Mocker.Resolve<IDatabase>();
            var tracks = db.Query<Book>(new SqlBuilder(db.DatabaseType)).ToList();

            foreach (var track in tracks)
            {
                Assert.IsFalse(track.BookFiles.IsLoaded);
                Assert.IsNotNull(track.BookFiles.Value);
                Assert.IsTrue(track.BookFiles.IsLoaded);
            }
        }

        [Test]
        public void should_explicit_load_everything_if_joined()
        {
            var db = Mocker.Resolve<IDatabase>();
            var files = MediaFileRepository.Query(db,
                                                  new SqlBuilder(db.DatabaseType)
                                                  .Join<BookFile, Edition>((t, a) => t.EditionId == a.Id)
                                                  .Join<Edition, Book>((e, b) => e.BookId == b.Id)
                                                  .Join<Book, Author>((book, author) => book.AuthorMetadataId == author.AuthorMetadataId)
                                                  .Join<Author, AuthorMetadata>((a, m) => a.AuthorMetadataId == m.Id));

            Assert.IsNotEmpty(files);
            foreach (var file in files)
            {
                Assert.IsTrue(file.Edition.IsLoaded);
                Assert.IsTrue(file.Author.IsLoaded);
                Assert.IsTrue(file.Author.Value.Metadata.IsLoaded);
            }
        }
    }
}
