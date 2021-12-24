using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Http;

namespace NzbDrone.Core.MetadataSource.Goodreads
{
    public interface IGoodreadsSearchProxy
    {
        public List<SearchJsonResource> Search(string query);
    }

    public class GoodreadsSearchProxy : IGoodreadsSearchProxy
    {
        private readonly ICachedHttpResponseService _cachedHttpClient;
        private readonly Logger _logger;
        private readonly IHttpRequestBuilderFactory _searchBuilder;

        public GoodreadsSearchProxy(ICachedHttpResponseService cachedHttpClient,
            Logger logger)
        {
            _cachedHttpClient = cachedHttpClient;
            _logger = logger;

            _searchBuilder = new HttpRequestBuilder("https://www.goodreads.com/book/auto_complete")
                .AddQueryParam("format", "json")
                .SetHeader("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.116 Safari/537.36")
                .KeepAlive()
                .CreateFactory();
        }

        public List<SearchJsonResource> Search(string query)
        {
            try
            {
                var httpRequest = _searchBuilder.Create()
                    .AddQueryParam("q", query)
                    .Build();

                var response = _cachedHttpClient.Get<List<SearchJsonResource>>(httpRequest, true, TimeSpan.FromDays(5));

                return response.Resource;
            }
            catch (HttpException)
            {
                throw new GoodreadsException("Search for '{0}' failed. Unable to communicate with Goodreads.", query);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, ex.Message);
                throw new GoodreadsException("Search for '{0}' failed. Invalid response received from Goodreads.", query);
            }
        }
    }
}
