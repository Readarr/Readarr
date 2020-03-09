using System;
using System.IO;
using System.Net;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Rest;

namespace NzbDrone.Core.Notifications.Calibre
{
    public interface ICalibreProxy
    {
        CalibreImportJob AddFile(BookFile book, CalibreSettings settings);
        CalibreBookData GetBookData(int calibreId, CalibreSettings settings);
        long ConvertBook(int calibreId, CalibreConversionOptions options, CalibreSettings settings);
        void GetListing(CalibreSettings settings);
    }

    public class CalibreProxy : ICalibreProxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public CalibreProxy(IHttpClient httpClient,
                            Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public CalibreImportJob AddFile(BookFile book, CalibreSettings settings)
        {
            var jobid = (int)(DateTime.UtcNow.Ticks % 1000000000);
            var addDuplicates = false;
            var path = book.Path;
            var filename = Path.GetFileName(path);
            var body = File.ReadAllBytes(path);

            _logger.Trace($"Read {body.Length} bytes from {path}");

            try
            {
                var builder = new HttpRequestBuilder($"{settings.Url}/cdb/add-book/{jobid}/{addDuplicates}/{filename}")
                    .Accept(HttpAccept.Json);
                builder.LogResponseContent = true;
                builder.NetworkCredential = new NetworkCredential(settings.Username, settings.Password);

                var request = builder.Build();
                request.SetContent(body);

                return _httpClient.Post<CalibreImportJob>(request).Resource;
            }
            catch (RestException ex)
            {
                throw new CalibreException("Unable to add file to calibre library: {0}", ex, ex.Message);
            }
        }

        public CalibreBookData GetBookData(int calibreId, CalibreSettings settings)
        {
            try
            {
                var builder = new HttpRequestBuilder($"{settings.Url}/conversion/book-data/{calibreId}")
                    .Accept(HttpAccept.Json);
                builder.NetworkCredential = new NetworkCredential(settings.Username, settings.Password);

                var request = builder.Build();

                return _httpClient.Get<CalibreBookData>(request).Resource;
            }
            catch (RestException ex)
            {
                throw new CalibreException("Unable to add file to calibre library: {0}", ex, ex.Message);
            }
        }

        public long ConvertBook(int calibreId, CalibreConversionOptions options, CalibreSettings settings)
        {
            try
            {
                var builder = new HttpRequestBuilder($"{settings.Url}/conversion/start/{calibreId}")
                    .Accept(HttpAccept.Json);
                builder.LogResponseContent = true;
                builder.NetworkCredential = new NetworkCredential(settings.Username, settings.Password);

                var request = builder.Build();
                request.SetContent(options.ToJson());

                return _httpClient.Post<long>(request).Resource;
            }
            catch (RestException ex)
            {
                throw new CalibreException("Unable to start calibre conversion: {0}", ex, ex.Message);
            }
        }

        public void GetListing(CalibreSettings settings)
        {
            try
            {
                var builder = new HttpRequestBuilder($"{settings.Url}/ajax/books")
                    .Accept(HttpAccept.Json);
                builder.NetworkCredential = new NetworkCredential(settings.Username, settings.Password);

                var request = builder.Build();
                var response = _httpClient.Execute(request);
            }
            catch (RestException ex)
            {
                throw new CalibreException("Unable to connect to calibre library: {0}", ex, ex.Message);
            }
        }
    }
}
