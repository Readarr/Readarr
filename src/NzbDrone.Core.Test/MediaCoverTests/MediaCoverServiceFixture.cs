using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Events;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaCoverTests
{
    [TestFixture]
    public class MediaCoverServiceFixture : CoreTest<MediaCoverService>
    {
        private Author _author;
        private Book _book;
        private Edition _edition;
        private HttpResponse _httpResponse;

        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<IAppFolderInfo>(new AppFolderInfo(Mocker.Resolve<IStartupContext>()));

            _author = Builder<Author>.CreateNew()
                .With(v => v.Id = 2)
                .With(v => v.Metadata.Value.Images = new List<MediaCover.MediaCover> { new MediaCover.MediaCover(MediaCoverTypes.Poster, "") })
                .Build();

            _edition = Builder<Edition>.CreateNew()
                .With(v => v.Id = 8)
                .With(v => v.Images = new List<MediaCover.MediaCover> { new MediaCover.MediaCover(MediaCoverTypes.Cover, "") })
                .With(v => v.Monitored = true)
                .Build();

            _book = Builder<Book>.CreateNew()
                .With(v => v.Id = 4)
                .With(v => v.Editions = new List<Edition> { _edition })
                .Build();

            _httpResponse = new HttpResponse(null, new HttpHeader(), "");
            Mocker.GetMock<IHttpClient>().Setup(c => c.Get(It.IsAny<HttpRequest>())).Returns(_httpResponse);
        }

        [TestCase(".png")]
        [TestCase(".jpg")]
        public void should_convert_cover_urls_to_local(string extension)
        {
            var covers = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover
                    {
                        Url = "http://dummy.com/test" + extension,
                        CoverType = MediaCoverTypes.Banner
                    }
                };

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FileGetLastWrite(It.IsAny<string>()))
                  .Returns(new DateTime(1234));

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Subject.ConvertToLocalUrls(12, MediaCoverEntity.Author, covers);

            covers.Single().Url.Should().Be("/MediaCover/12/banner" + extension + "?lastWrite=1234");
        }

        [TestCase(".png")]
        [TestCase(".jpg")]
        public void convert_to_local_url_should_not_change_extension(string extension)
        {
            var covers = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover
                    {
                        Url = "http://dummy.com/test" + extension,
                        CoverType = MediaCoverTypes.Banner
                    }
                };

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FileGetLastWrite(It.IsAny<string>()))
                  .Returns(new DateTime(1234));

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Subject.ConvertToLocalUrls(12, MediaCoverEntity.Author, covers);

            covers.Single().Extension.Should().Be(extension);
        }

        [TestCase(".png")]
        [TestCase(".jpg")]
        public void should_convert_book_cover_urls_to_local(string extension)
        {
            var covers = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover
                    {
                        Url = "http://dummy.com/test" + extension,
                        CoverType = MediaCoverTypes.Disc
                    }
                };

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FileGetLastWrite(It.IsAny<string>()))
                  .Returns(new DateTime(1234));

            Mocker.GetMock<IDiskProvider>().Setup(c => c.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Subject.ConvertToLocalUrls(6, MediaCoverEntity.Book, covers);

            covers.Single().Url.Should().Be("/MediaCover/Books/6/disc" + extension + "?lastWrite=1234");
        }

        [TestCase(".png")]
        [TestCase(".jpg")]
        public void should_convert_media_urls_to_local_without_time_if_file_doesnt_exist(string extension)
        {
            var covers = new List<MediaCover.MediaCover>
                {
                    new MediaCover.MediaCover
                    {
                        Url = "http://dummy.com/test" + extension,
                        CoverType = MediaCoverTypes.Banner
                    }
                };

            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "Media", "NonExistant.mp4");
            var fileInfo = new FileInfo(path);

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.GetFileInfo(It.IsAny<string>()))
                .Returns((FileInfoBase)fileInfo);

            Subject.ConvertToLocalUrls(12, MediaCoverEntity.Author, covers);

            covers.Single().Url.Should().Be("/MediaCover/12/banner" + extension);
        }

        [Test]
        public void should_resize_covers_if_main_downloaded()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<DateTime?>(), It.IsAny<long?>(), It.IsAny<string>()))
                  .Returns(false);

            Mocker.GetMock<IBookService>()
                  .Setup(v => v.GetBooksByAuthor(It.IsAny<int>()))
                  .Returns(new List<Book> { _book });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Subject.HandleAsync(new AuthorRefreshCompleteEvent(_author));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [Test]
        public void should_resize_covers_if_missing()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<DateTime?>(), It.IsAny<long?>(), It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IBookService>()
                  .Setup(v => v.GetBooksByAuthor(It.IsAny<int>()))
                  .Returns(new List<Book> { _book });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(false);

            Subject.HandleAsync(new AuthorRefreshCompleteEvent(_author));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [Test]
        public void should_not_resize_covers_if_exists()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<DateTime?>(), It.IsAny<long?>(), It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IBookService>()
                  .Setup(v => v.GetBooksByAuthor(It.IsAny<int>()))
                  .Returns(new List<Book> { _book });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetFileSize(It.IsAny<string>()))
                  .Returns(1000);

            Subject.HandleAsync(new AuthorRefreshCompleteEvent(_author));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_resize_covers_if_existing_is_empty()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<DateTime?>(), It.IsAny<long?>(), It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IBookService>()
                  .Setup(v => v.GetBooksByAuthor(It.IsAny<int>()))
                  .Returns(new List<Book> { _book });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetFileSize(It.IsAny<string>()))
                  .Returns(0);

            Subject.HandleAsync(new AuthorRefreshCompleteEvent(_author));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [Test]
        public void should_log_error_if_resize_failed()
        {
            Mocker.GetMock<ICoverExistsSpecification>()
                  .Setup(v => v.AlreadyExists(It.IsAny<DateTime?>(), It.IsAny<long?>(), It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(It.IsAny<string>()))
                  .Returns(false);

            Mocker.GetMock<IBookService>()
                  .Setup(v => v.GetBooksByAuthor(It.IsAny<int>()))
                  .Returns(new List<Book> { _book });

            Mocker.GetMock<IImageResizer>()
                  .Setup(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                  .Throws<ApplicationException>();

            Subject.HandleAsync(new AuthorRefreshCompleteEvent(_author));

            Mocker.GetMock<IImageResizer>()
                  .Verify(v => v.Resize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }
    }
}
