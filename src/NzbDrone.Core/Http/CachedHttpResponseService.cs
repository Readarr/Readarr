using System;
using System.Net;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Http
{
    public interface ICachedHttpResponseService
    {
        HttpResponse Get(HttpRequest request, bool useCache, TimeSpan ttl);
        HttpResponse<T> Get<T>(HttpRequest request, bool useCache, TimeSpan ttl)
            where T : new();
    }

    public class CachedHttpResponseService : ICachedHttpResponseService
    {
        private readonly ICachedHttpResponseRepository _repo;
        private readonly IHttpClient _httpClient;

        public CachedHttpResponseService(ICachedHttpResponseRepository httpResponseRepository,
                                         IHttpClient httpClient)
        {
            _repo = httpResponseRepository;
            _httpClient = httpClient;
        }

        public HttpResponse Get(HttpRequest request, bool useCache, TimeSpan ttl)
        {
            var cached = _repo.FindByUrl(request.Url.ToString());

            if (useCache && cached != null && cached.Expiry > DateTime.UtcNow)
            {
                return new HttpResponse(request, new HttpHeader(), cached.Value, (HttpStatusCode)cached.StatusCode);
            }

            var result = _httpClient.Get(request);

            if (!result.HasHttpError)
            {
                if (cached == null)
                {
                    cached = new CachedHttpResponse
                    {
                        Url = request.Url.ToString(),
                    };
                }

                var now = DateTime.UtcNow;

                cached.LastRefresh = now;
                cached.Expiry = now.Add(ttl);
                cached.Value = result.Content;
                cached.StatusCode = (int)result.StatusCode;

                _repo.Upsert(cached);
            }

            return result;
        }

        public HttpResponse<T> Get<T>(HttpRequest request, bool useCache, TimeSpan ttl)
            where T : new()
        {
            var response = Get(request, useCache, ttl);
            return new HttpResponse<T>(response);
        }
    }
}
