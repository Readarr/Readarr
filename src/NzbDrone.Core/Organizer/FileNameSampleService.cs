using System.Collections.Generic;
using NzbDrone.Core.Books;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Organizer
{
    public interface IFilenameSampleService
    {
        SampleResult GetStandardTrackSample(NamingConfig nameSpec);
        SampleResult GetMultiDiscTrackSample(NamingConfig nameSpec);
        string GetAuthorFolderSample(NamingConfig nameSpec);
    }

    public class FileNameSampleService : IFilenameSampleService
    {
        private readonly IBuildFileNames _buildFileNames;

        private static Author _standardAuthor;
        private static Book _standardBook;
        private static Edition _standardEdition;
        private static BookFile _singleTrackFile;
        private static BookFile _multiTrackFile;
        private static List<CustomFormat> _customFormats;

        public FileNameSampleService(IBuildFileNames buildFileNames)
        {
            _buildFileNames = buildFileNames;

            _standardAuthor = new Author
            {
                Metadata = new AuthorMetadata
                {
                    Name = "The Author Name",
                    Disambiguation = "US Author",
                    NameLastFirst = "Last name, First name"
                }
            };

            var series = new Series
            {
                Title = "Series Title"
            };

            var seriesLink = new SeriesBookLink
            {
                Position = "1",
                Series = series
            };

            _standardBook = new Book
            {
                Title = "The Book Title",
                ReleaseDate = System.DateTime.Today,
                Author = _standardAuthor,
                AuthorMetadata = _standardAuthor.Metadata.Value,
                SeriesLinks = new List<SeriesBookLink> { seriesLink }
            };

            _standardEdition = new Edition
            {
                Title = "The Edition Title",
                Book = _standardBook
            };

            _customFormats = new List<CustomFormat>
            {
                new CustomFormat
                {
                    Name = "Surround Sound",
                    IncludeCustomFormatWhenRenaming = true
                },
                new CustomFormat
                {
                    Name = "x264",
                    IncludeCustomFormatWhenRenaming = true
                }
            };

            var mediaInfo = new MediaInfoModel()
            {
                AudioFormat = "Flac Audio",
                AudioChannels = 2,
                AudioBitrate = 875,
                AudioBits = 24,
                AudioSampleRate = 44100
            };

            _singleTrackFile = new BookFile
            {
                Quality = new QualityModel(Quality.MP3, new Revision(2)),
                Path = "/music/Author.Name.Book.Name.TrackNum.Track.Title.MP3256.mp3",
                SceneName = "Author.Name.Book.Name.TrackNum.Track.Title.MP3256",
                ReleaseGroup = "RlsGrp",
                MediaInfo = mediaInfo,
                Edition = _standardEdition,
                Part = 1,
                PartCount = 1
            };

            _multiTrackFile = new BookFile
            {
                Quality = new QualityModel(Quality.MP3, new Revision(2)),
                Path = "/music/Author.Name.Book.Name.TrackNum.Track.Title.MP3256.mp3",
                SceneName = "Author.Name.Book.Name.TrackNum.Track.Title.MP3256",
                ReleaseGroup = "RlsGrp",
                MediaInfo = mediaInfo,
                Edition = _standardEdition,
                Part = 1,
                PartCount = 2
            };
        }

        public SampleResult GetStandardTrackSample(NamingConfig nameSpec)
        {
            var result = new SampleResult
            {
                FileName = BuildTrackSample(_standardAuthor, _singleTrackFile, nameSpec),
                Author = _standardAuthor,
                Book = _standardBook,
                BookFile = _singleTrackFile
            };

            return result;
        }

        public SampleResult GetMultiDiscTrackSample(NamingConfig nameSpec)
        {
            var result = new SampleResult
            {
                FileName = BuildTrackSample(_standardAuthor, _multiTrackFile, nameSpec),
                Author = _standardAuthor,
                Book = _standardBook,
                BookFile = _singleTrackFile
            };

            return result;
        }

        public string GetAuthorFolderSample(NamingConfig nameSpec)
        {
            return _buildFileNames.GetAuthorFolder(_standardAuthor, nameSpec);
        }

        private string BuildTrackSample(Author author, BookFile bookFile, NamingConfig nameSpec)
        {
            try
            {
                return _buildFileNames.BuildBookFileName(author, bookFile.Edition.Value, bookFile, nameSpec, _customFormats);
            }
            catch (NamingFormatException)
            {
                return string.Empty;
            }
        }
    }
}
