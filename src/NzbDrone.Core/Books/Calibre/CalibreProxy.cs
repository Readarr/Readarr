using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Books.Calibre
{
    public interface ICalibreProxy
    {
        BookFile AddAndConvert(BookFile file, CalibreSettings settings);
        void DeleteBook(BookFile book, CalibreSettings settings);
        void DeleteBooks(List<BookFile> books, CalibreSettings settings);
        void RemoveFormats(int calibreId, IEnumerable<string> formats, CalibreSettings settings);
        void SetFields(BookFile file, CalibreSettings settings, bool updateCover = true, bool embed = false);
        List<string> GetAllBookFilePaths(CalibreSettings settings);
        CalibreBook GetBook(int calibreId, CalibreSettings settings);
        List<CalibreBook> GetBooks(List<int> calibreId, CalibreSettings settings);
        void Test(CalibreSettings settings);
    }

    public class CalibreProxy : ICalibreProxy
    {
        private const int PAGE_SIZE = 750;

        private readonly IHttpClient _httpClient;
        private readonly IMapCoversToLocal _mediaCoverService;
        private readonly IRemotePathMappingService _pathMapper;
        private readonly IRootFolderWatchingService _rootFolderWatchingService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IConfigService _configService;
        private readonly Logger _logger;
        private readonly ICached<CalibreBook> _bookCache;

        public CalibreProxy(IHttpClient httpClient,
                            IMapCoversToLocal mediaCoverService,
                            IRemotePathMappingService pathMapper,
                            IRootFolderWatchingService rootFolderWatchingService,
                            IMediaFileService mediaFileService,
                            IConfigService configService,
                            ICacheManager cacheManager,
                            Logger logger)
        {
            _httpClient = httpClient;
            _mediaCoverService = mediaCoverService;
            _pathMapper = pathMapper;
            _rootFolderWatchingService = rootFolderWatchingService;
            _mediaFileService = mediaFileService;
            _configService = configService;
            _bookCache = cacheManager.GetCache<CalibreBook>(GetType());
            _logger = logger;
        }

        public static string GetOriginalFormat(Dictionary<string, CalibreBookFormat> formats)
        {
            return formats
                .Where(x => MediaFileExtensions.TextExtensions.Contains("." + x.Key))
                .OrderBy(f => f.Value.LastModified)
                .FirstOrDefault().Value?.Path;
        }

        public BookFile AddAndConvert(BookFile file, CalibreSettings settings)
        {
            _logger.Trace($"Importing to calibre: {file.Path} calibre id: {file.CalibreId}");

            if (file.CalibreId == 0)
            {
                var import = AddBook(file, settings);
                file.CalibreId = import.Id;
            }
            else
            {
                AddFormat(file, settings);
            }

            SetFields(file, settings, true, _configService.EmbedMetadata);

            if (settings.OutputFormat.IsNotNullOrWhiteSpace())
            {
                _logger.Trace($"Getting book data for {file.CalibreId}");
                var options = GetBookData(file.CalibreId, settings);
                var inputFormat = file.Quality.Quality.Name.ToUpper();

                options.Conversion_options.Input_fmt = inputFormat;

                var formats = settings.OutputFormat.Split(',').Select(x => x.Trim());
                foreach (var format in formats)
                {
                    if (format.ToLower() == inputFormat ||
                        options.Input_formats.Contains(format, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    options.Conversion_options.Output_fmt = format;

                    if (settings.OutputProfile != (int)CalibreProfile.@default)
                    {
                        options.Conversion_options.Options.Output_profile = ((CalibreProfile)settings.OutputProfile).ToString();
                    }

                    _logger.Trace($"Starting conversion to {format}");

                    _rootFolderWatchingService.ReportFileSystemChangeBeginning(Path.ChangeExtension(file.Path, format));
                    ConvertBook(file.CalibreId, options.Conversion_options, settings);
                }
            }

            return file;
        }

        private CalibreImportJob AddBook(BookFile book, CalibreSettings settings)
        {
            var jobid = (int)(DateTime.UtcNow.Ticks % 1000000000);
            var addDuplicates = 1;
            var path = book.Path;
            var filename = $"$dummy{Path.GetExtension(path)}";
            var body = File.ReadAllBytes(path);

            _logger.Trace($"Read {body.Length} bytes from {path}");

            try
            {
                var builder = GetBuilder($"cdb/add-book/{jobid}/{addDuplicates}/{filename}/{settings.Library}", settings);

                var request = builder.Build();
                request.SetContent(body);

                var response = _httpClient.Post<CalibreImportJob>(request).Resource;

                if (response.Id == 0)
                {
                    throw new CalibreException("Calibre rejected duplicate book");
                }

                return response;
            }
            catch (HttpException ex)
            {
                throw new CalibreException("Unable to add file to Calibre library: {0}", ex, ex.Message);
            }
        }

        public void DeleteBook(BookFile book, CalibreSettings settings)
        {
            var request = GetBuilder($"cdb/delete-books/{book.CalibreId}/{settings.Library}", settings).Build();
            _httpClient.Post(request);
        }

        public void DeleteBooks(List<BookFile> books, CalibreSettings settings)
        {
            var idString = books.Where(x => x.CalibreId != 0).Select(x => x.CalibreId).ConcatToString(",");
            var request = GetBuilder($"cdb/delete-books/{idString}/{settings.Library}", settings).Build();
            _httpClient.Post(request);
        }

        private void AddFormat(BookFile file, CalibreSettings settings)
        {
            var format = Path.GetExtension(file.Path);
            var bookData = Convert.ToBase64String(File.ReadAllBytes(file.Path));

            var payload = new CalibreChangesPayload
            {
                LoadedBookIds = new List<int> { file.CalibreId },
                Changes = new CalibreChanges
                {
                    AddedFormats = new List<CalibreAddFormat>
                    {
                        new CalibreAddFormat
                        {
                            Ext = format,
                            Data = bookData
                        }
                    }
                }
            };

            ExecuteSetFields(file.CalibreId, payload, settings);
        }

        public void RemoveFormats(int calibreId, IEnumerable<string> formats, CalibreSettings settings)
        {
            var payload = new CalibreChangesPayload
            {
                LoadedBookIds = new List<int> { calibreId },
                Changes = new CalibreChanges
                {
                    RemovedFormats = formats.ToList()
                }
            };

            ExecuteSetFields(calibreId, payload, settings);
        }

        public void SetFields(BookFile file, CalibreSettings settings, bool updateCover = true, bool embed = false)
        {
            var edition = file.Edition.Value;
            var book = edition.Book.Value;
            var serieslink = book.SeriesLinks.Value.OrderBy(x => x.SeriesPosition).FirstOrDefault(x => x.Series.Value.Title.IsNotNullOrWhiteSpace());

            var series = serieslink?.Series.Value;
            double? seriesIndex = null;
            if (double.TryParse(serieslink?.Position, out var index))
            {
                _logger.Trace($"Parsed {serieslink?.Position} as {index}");
                seriesIndex = index;
            }

            _logger.Trace($"Book: {book} Series: {series?.Title}, Position: {seriesIndex}");

            var cover = edition.Images.FirstOrDefault(x => x.CoverType == MediaCoverTypes.Cover);
            string image = null;
            if (cover != null)
            {
                var imageFile = _mediaCoverService.GetCoverPath(edition.BookId, MediaCoverEntity.Book, cover.CoverType, cover.Extension, null);

                if (File.Exists(imageFile))
                {
                    var imageData = File.ReadAllBytes(imageFile);
                    if (CalibreImageValidator.IsValidImage(imageData))
                    {
                        image = Convert.ToBase64String(imageData);
                    }
                }
            }

            var textInfo = CultureInfo.InvariantCulture.TextInfo;
            var genres = book.Genres.Select(x => textInfo.ToTitleCase(x.Replace('-', ' '))).ToList();

            var payload = new CalibreChangesPayload
            {
                LoadedBookIds = new List<int> { file.CalibreId },
                Changes = new CalibreChanges
                {
                    Title = edition.Title,
                    Authors = new List<string> { file.Author.Value.Name },
                    Cover = updateCover ? image : null,
                    PubDate = book.ReleaseDate,
                    Publisher = edition.Publisher,
                    Languages = edition.Language.CanonicalizeLanguage(),
                    Tags = genres,
                    Comments = edition.Overview,
                    Rating = (int)(edition.Ratings.Value * 2),
                    Identifiers = new Dictionary<string, string>
                    {
                        { "isbn", edition.Isbn13 },
                        { "asin", edition.Asin },
                        { "goodreads", edition.ForeignEditionId }
                    },
                    Series = series?.Title,
                    SeriesIndex = seriesIndex
                }
            };

            ExecuteSetFields(file.CalibreId, payload, settings);

            // updating the calibre metadata may have renamed the file, so track that
            var updated = GetBook(file.CalibreId, settings);

            var updatedPath = GetOriginalFormat(updated.Formats);

            if (updatedPath != file.Path)
            {
                _rootFolderWatchingService.ReportFileSystemChangeBeginning(updatedPath);
                file.Path = updatedPath;
            }

            var fileInfo = new FileInfo(file.Path);
            file.Size = fileInfo.Length;
            file.Modified = fileInfo.LastWriteTimeUtc;

            if (file.Id > 0)
            {
                _mediaFileService.Update(file);
            }

            if (embed)
            {
                EmbedMetadata(file, settings);
            }
        }

        private void ExecuteSetFields(int id, CalibreChangesPayload payload, CalibreSettings settings)
        {
            var builder = GetBuilder($"cdb/set-fields/{id}/{settings.Library}", settings)
                .Post()
                .SetHeader("Content-Type", "application/json");

            var request = builder.Build();
            request.SetContent(payload.ToJson());

            _httpClient.Execute(request);
        }

        private void EmbedMetadata(BookFile file, CalibreSettings settings)
        {
            _rootFolderWatchingService.ReportFileSystemChangeBeginning(file.Path);

            var request = GetBuilder($"cdb/cmd/embed_metadata", settings)
                .AddQueryParam("library_id", settings.Library)
                .Post()
                .SetHeader("Content-Type", "application/json")
                .Build();

            request.SetContent($"[{file.CalibreId}, null]");
            _httpClient.Execute(request);

            PollEmbedStatus(file, settings);
        }

        private void PollEmbedStatus(BookFile file, CalibreSettings settings)
        {
            var previous = new FileInfo(file.Path);
            Thread.Sleep(100);

            FileInfo current = null;

            var i = 0;
            while (i++ < 20)
            {
                current = new FileInfo(file.Path);

                if (current.LastWriteTimeUtc == previous.LastWriteTimeUtc &&
                    current.LastWriteTimeUtc != file.Modified)
                {
                    break;
                }

                previous = current;
                Thread.Sleep(1000);
            }

            file.Size = current.Length;
            file.Modified = current.LastWriteTimeUtc;

            if (file.Id > 0)
            {
                _mediaFileService.Update(file);
            }
        }

        private CalibreBookData GetBookData(int calibreId, CalibreSettings settings)
        {
            try
            {
                var request = GetBuilder($"conversion/book-data/{calibreId}", settings)
                    .AddQueryParam("library_id", settings.Library)
                    .Build();

                return _httpClient.Get<CalibreBookData>(request).Resource;
            }
            catch (HttpException ex)
            {
                throw new CalibreException("Unable to add file to Calibre library: {0}", ex, ex.Message);
            }
        }

        private long ConvertBook(int calibreId, CalibreConversionOptions options, CalibreSettings settings)
        {
            try
            {
                var request = GetBuilder($"conversion/start/{calibreId}", settings)
                    .AddQueryParam("library_id", settings.Library)
                    .Build();
                request.SetContent(options.ToJson());

                var jobId = _httpClient.Post<long>(request).Resource;

                // Run async task to check if conversion complete
                _ = PollConvertStatus(jobId, settings);

                return jobId;
            }
            catch (HttpException ex)
            {
                throw new CalibreException("Unable to start Calibre conversion: {0}", ex, ex.Message);
            }
        }

        public CalibreBook GetBook(int calibreId, CalibreSettings settings)
        {
            try
            {
                var builder = GetBuilder($"ajax/book/{calibreId}/{settings.Library}", settings);

                var request = builder.Build();
                var book = _httpClient.Get<CalibreBook>(request).Resource;

                foreach (var format in book.Formats.Values)
                {
                    format.Path = _pathMapper.RemapRemoteToLocal(settings.Host, new OsPath(format.Path)).FullPath;
                }

                return book;
            }
            catch (HttpException ex)
            {
                throw new CalibreException("Unable to connect to Calibre library: {0}", ex, ex.Message);
            }
        }

        public List<CalibreBook> GetBooks(List<int> calibreIds, CalibreSettings settings)
        {
            var builder = GetBuilder($"ajax/books/{settings.Library}", settings);
            builder.LogResponseContent = false;
            builder.AddQueryParam("ids", calibreIds.ConcatToString(","));

            var request = builder.Build();

            try
            {
                var response = _httpClient.Get<Dictionary<int, CalibreBook>>(request);
                var result = response.Resource.Values.ToList();

                foreach (var book in result)
                {
                    foreach (var format in book.Formats.Values)
                    {
                        format.Path = _pathMapper.RemapRemoteToLocal(settings.Host, new OsPath(format.Path)).FullPath;
                    }
                }

                return result;
            }
            catch (HttpException ex)
            {
                throw new CalibreException("Unable to connect to Calibre library: {0}", ex, ex.Message);
            }
        }

        public List<string> GetAllBookFilePaths(CalibreSettings settings)
        {
            var ids = GetAllBookIds(settings);
            var result = new List<string>();

            var offset = 0;

            while (offset < ids.Count)
            {
                var builder = GetBuilder($"ajax/books/{settings.Library}", settings);
                builder.LogResponseContent = false;
                builder.AddQueryParam("ids", ids.Skip(offset).Take(PAGE_SIZE).ConcatToString(","));

                var request = builder.Build();
                try
                {
                    var response = _httpClient.Get<Dictionary<int, CalibreBook>>(request);
                    foreach (var book in response.Resource.Values)
                    {
                        var remotePath = GetOriginalFormat(book?.Formats);

                        if (remotePath == null)
                        {
                            continue;
                        }

                        var localPath = _pathMapper.RemapRemoteToLocal(settings.Host, new OsPath(remotePath)).FullPath;
                        result.Add(localPath);

                        _bookCache.Set(localPath, book);
                    }
                }
                catch (HttpException ex)
                {
                    throw new CalibreException("Unable to connect to Calibre library: {0}", ex, ex.Message);
                }

                offset += PAGE_SIZE;
            }

            return result;
        }

        public List<int> GetAllBookIds(CalibreSettings settings)
        {
            // the magic string is 'allbooks' converted to hex
            var builder = GetBuilder($"/ajax/category/616c6c626f6f6b73/{settings.Library}", settings);
            var offset = 0;

            var ids = new List<int>();

            while (true)
            {
                var result = GetPaged<CalibreCategory>(builder, PAGE_SIZE, offset);
                if (!result.Resource.BookIds.Any())
                {
                    break;
                }

                offset += PAGE_SIZE;
                ids.AddRange(result.Resource.BookIds);
            }

            return ids;
        }

        private HttpResponse<T> GetPaged<T>(HttpRequestBuilder builder, int count, int offset)
            where T : new()
        {
            builder.AddQueryParam("num", count, replace: true);
            builder.AddQueryParam("offset", offset, replace: true);

            var request = builder.Build();

            try
            {
                return _httpClient.Get<T>(request);
            }
            catch (HttpException ex)
            {
                throw new CalibreException("Unable to connect to Calibre library: {0}", ex, ex.Message);
            }
        }

        private CalibreLibraryInfo GetLibraryInfo(CalibreSettings settings)
        {
            var builder = GetBuilder($"ajax/library-info", settings);
            var request = builder.Build();
            var response = _httpClient.Get<CalibreLibraryInfo>(request);

            return response.Resource;
        }

        private bool CalibreLoginEnabled(CalibreSettings settings)
        {
            var builder = GetBuilder($"/book-get-last-read-position/{settings.Library}/1", settings);
            builder.SuppressHttpError = true;

            var request = builder.Build();
            var response = _httpClient.Get(request);

            return response.StatusCode != HttpStatusCode.NotFound;
        }

        private HttpRequestBuilder GetBuilder(string relativePath, CalibreSettings settings)
        {
            var baseUrl = HttpRequestBuilder.BuildBaseUrl(settings.UseSsl, settings.Host, settings.Port, settings.UrlBase);
            baseUrl = HttpUri.CombinePath(baseUrl, relativePath);

            var builder = new HttpRequestBuilder(baseUrl)
                .Accept(HttpAccept.Json);

            builder.LogResponseContent = true;

            if (settings.Username.IsNotNullOrWhiteSpace())
            {
                builder.NetworkCredential = new NetworkCredential(settings.Username, settings.Password);
            }

            return builder;
        }

        private async Task PollConvertStatus(long jobId, CalibreSettings settings)
        {
            var request = GetBuilder($"/conversion/status/{jobId}", settings)
                .AddQueryParam("library_id", settings.Library)
                .Build();

            while (true)
            {
                var status = _httpClient.Get<CalibreConversionStatus>(request).Resource;

                if (!status.Running)
                {
                    if (!status.Ok)
                    {
                        _logger.Warn("Calibre conversion failed.\n{0}\n{1}", status.Traceback, status.Log);
                    }

                    return;
                }

                await Task.Delay(2000);
            }
        }

        public void Test(CalibreSettings settings)
        {
            var failures = new List<ValidationFailure> { TestCalibre(settings) };
            var validationResult = new ValidationResult(failures);
            var result = new NzbDroneValidationResult(validationResult.Errors);

            if (!result.IsValid || result.HasWarnings)
            {
                throw new ValidationException(result.Failures);
            }
        }

        private ValidationFailure TestCalibre(CalibreSettings settings)
        {
            var authRequired = settings.Host != "127.0.0.1" && settings.Host != "::1" && settings.Host != "localhost";

            if (authRequired && settings.Username.IsNullOrWhiteSpace())
            {
                return new NzbDroneValidationFailure("Username", "Username required")
                {
                    DetailedDescription = "A username/password is required for non-local Calibre servers to allow write access"
                };
            }

            var builder = GetBuilder("", settings);
            builder.Accept(HttpAccept.Html);
            builder.SuppressHttpError = true;
            builder.AllowAutoRedirect = true;

            var request = builder.Build();
            request.LogResponseContent = false;
            HttpResponse response;

            try
            {
                response = _httpClient.Execute(request);
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Unable to connect to Calibre");
                if (ex.Status == WebExceptionStatus.ConnectFailure)
                {
                    return new NzbDroneValidationFailure("Host", "Unable to connect")
                    {
                        DetailedDescription = "Please verify the hostname and port."
                    };
                }

                return new NzbDroneValidationFailure(string.Empty, "Unknown exception: " + ex.Message);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new ValidationFailure("Host", "Could not connect");
            }

            if (response.Content.Contains(@"guac-login"))
            {
                return new ValidationFailure("Port", "Bad port. This is the container's remote Calibre GUI, not the Calibre content server.  Try mapping port 8081.");
            }

            if (response.Content.Contains("Calibre-Web"))
            {
                return new ValidationFailure("Port", "This is a Calibre-Web server, not the required Calibre content server.  See https://manual.calibre-ebook.com/server.html");
            }

            if (!response.Content.Contains(@"<title>calibre</title>"))
            {
                return new ValidationFailure("Port", "Not a valid Calibre content server.  See https://manual.calibre-ebook.com/server.html");
            }

            CalibreLibraryInfo libraryInfo;
            try
            {
                libraryInfo = GetLibraryInfo(settings);
            }
            catch (HttpException e)
            {
                if (e.Response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new NzbDroneValidationFailure("Username", "Authentication failure")
                    {
                        DetailedDescription = "Please verify your username and password."
                    };
                }
                else
                {
                    return new NzbDroneValidationFailure(string.Empty, "Unknown exception: " + e.Message);
                }
            }

            if (settings.Library.IsNullOrWhiteSpace())
            {
                settings.Library = libraryInfo.DefaultLibrary;
            }

            // now that we have library info, double check if auth is actually enabled calibre side.  If not, we'll get a 404 back.
            // https://github.com/kovidgoyal/calibre/blob/bf53bbf07a6ced728bf6a87d097fb6eb8c67e4e0/src/calibre/srv/books.py#L196
            if (authRequired && !CalibreLoginEnabled(settings))
            {
                return new ValidationFailure("Host", "Remote calibre server must have authentication enabled to allow Readarr write access");
            }

            if (!libraryInfo.LibraryMap.ContainsKey(settings.Library))
            {
                return new ValidationFailure("Library", "Not a valid library in calibre");
            }

            return null;
        }
    }
}
