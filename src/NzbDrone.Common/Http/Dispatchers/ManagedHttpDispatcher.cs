using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http.Proxy;

namespace NzbDrone.Common.Http.Dispatchers
{
    public class ManagedHttpDispatcher : IHttpDispatcher
    {
        private const string NO_PROXY_KEY = "no-proxy";

        private readonly IHttpProxySettingsProvider _proxySettingsProvider;
        private readonly ICreateManagedWebProxy _createManagedWebProxy;
        private readonly IUserAgentBuilder _userAgentBuilder;
        private readonly ICached<System.Net.Http.HttpClient> _httpClientCache;
        private readonly ICached<CredentialCache> _credentialCache;
        private readonly Logger _logger;

        public ManagedHttpDispatcher(IHttpProxySettingsProvider proxySettingsProvider,
            ICreateManagedWebProxy createManagedWebProxy,
            IUserAgentBuilder userAgentBuilder,
            ICacheManager cacheManager,
            Logger logger)
        {
            _proxySettingsProvider = proxySettingsProvider;
            _createManagedWebProxy = createManagedWebProxy;
            _userAgentBuilder = userAgentBuilder;
            _logger = logger;

            _httpClientCache = cacheManager.GetCache<System.Net.Http.HttpClient>(typeof(ManagedHttpDispatcher), "httpclient");
            _credentialCache = cacheManager.GetCache<CredentialCache>(typeof(ManagedHttpDispatcher), "credentialcache");
        }

        public HttpResponse GetResponse(HttpRequest request, CookieContainer cookies)
        {
            var requestMessage = new HttpRequestMessage(request.Method, (Uri)request.Url);
            requestMessage.Headers.UserAgent.ParseAdd(_userAgentBuilder.GetUserAgent(request.UseSimplifiedUserAgent));
            requestMessage.Headers.ConnectionClose = !request.ConnectionKeepAlive;

            var cookieHeader = cookies.GetCookieHeader((Uri)request.Url);
            if (cookieHeader.IsNotNullOrWhiteSpace())
            {
                requestMessage.Headers.Add("Cookie", cookieHeader);
            }

            if (request.Credentials != null)
            {
                if (request.Credentials is BasicNetworkCredential bc)
                {
                    // Manually set header to avoid initial challenge response
                    var authInfo = bc.UserName + ":" + bc.Password;
                    authInfo = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(authInfo));
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authInfo);
                }
                else if (request.Credentials is NetworkCredential nc)
                {
                    var creds = GetCredentialCache();
                    creds.Remove((Uri)request.Url, "Digest");
                    creds.Add((Uri)request.Url, "Digest", nc);
                }
            }

            using var cts = new CancellationTokenSource();
            if (request.RequestTimeout != TimeSpan.Zero)
            {
                cts.CancelAfter(request.RequestTimeout);
            }
            else
            {
                // The default for System.Net.Http.HttpClient
                cts.CancelAfter(TimeSpan.FromSeconds(100));
            }

            if (request.Headers != null)
            {
                AddRequestHeaders(requestMessage, request.Headers);
            }

            var httpClient = GetClient(request.Url);

            HttpResponseMessage responseMessage;

            try
            {
                if (request.ContentData != null)
                {
                    var content = new ByteArrayContent(request.ContentData);
                    content.Headers.Remove("Content-Type");
                    if (request.Headers.ContentType.IsNotNullOrWhiteSpace())
                    {
                        content.Headers.Add("Content-Type", request.Headers.ContentType);
                    }

                    requestMessage.Content = content;
                }

                responseMessage = httpClient.Send(requestMessage, cts.Token);
            }
            catch (HttpRequestException e)
            {
                _logger.Error(e, "HttpClient error");
                throw;
            }

            byte[] data = null;

            using (var responseStream = responseMessage.Content.ReadAsStream())
            {
                if (responseStream != null && responseStream != Stream.Null)
                {
                    try
                    {
                        if (request.ResponseStream != null && responseMessage.StatusCode == HttpStatusCode.OK)
                        {
                            // A target ResponseStream was specified, write to that instead.
                            // But only on the OK status code, since we don't want to write failures and redirects.
                            responseStream.CopyTo(request.ResponseStream);
                        }
                        else
                        {
                            data = responseStream.ToBytes();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new WebException("Failed to read complete http response", ex, WebExceptionStatus.ReceiveFailure, null);
                    }
                }
            }

            return new HttpResponse(request, new HttpHeader(responseMessage.Headers), data, responseMessage.StatusCode);
        }

        protected virtual System.Net.Http.HttpClient GetClient(HttpUri uri)
        {
            var proxySettings = _proxySettingsProvider.GetProxySettings(uri);

            var key = proxySettings?.Key ?? NO_PROXY_KEY;

            return _httpClientCache.Get(key, () => CreateHttpClient(proxySettings));
        }

        protected virtual System.Net.Http.HttpClient CreateHttpClient(HttpProxySettings proxySettings)
        {
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Brotli,
                UseCookies = false, // sic - we don't want to use a shared cookie container
                AllowAutoRedirect = false,
                Credentials = GetCredentialCache(),
                PreAuthenticate = true
            };

            if (proxySettings != null)
            {
                handler.Proxy = _createManagedWebProxy.GetWebProxy(proxySettings);
            }

            var client = new System.Net.Http.HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };

            return client;
        }

        protected virtual void AddRequestHeaders(HttpRequestMessage webRequest, HttpHeader headers)
        {
            foreach (var header in headers)
            {
                switch (header.Key)
                {
                    case "Accept":
                        webRequest.Headers.Accept.ParseAdd(header.Value);
                        break;
                    case "Connection":
                        webRequest.Headers.Connection.Clear();
                        webRequest.Headers.Connection.Add(header.Value);
                        break;
                    case "Content-Length":
                        webRequest.Headers.Add("Content-Length", header.Value);
                        break;
                    case "Content-Type":
                        webRequest.Headers.Remove("Content-Type");
                        webRequest.Headers.Add("Content-Type", header.Value);
                        break;
                    case "Date":
                        webRequest.Headers.Remove("Date");
                        webRequest.Headers.Date = HttpHeader.ParseDateTime(header.Value);
                        break;
                    case "Expect":
                        webRequest.Headers.Expect.ParseAdd(header.Value);
                        break;
                    case "Host":
                        webRequest.Headers.Host = header.Value;
                        break;
                    case "If-Modified-Since":
                        webRequest.Headers.IfModifiedSince = HttpHeader.ParseDateTime(header.Value);
                        break;
                    case "Referer":
                        webRequest.Headers.Add("Referer", header.Value);
                        break;
                    case "Transfer-Encoding":
                        webRequest.Headers.TransferEncoding.ParseAdd(header.Value);
                        break;
                    case "User-Agent":
                        webRequest.Headers.UserAgent.ParseAdd(header.Value);
                        break;
                    case "Proxy-Connection":
                        throw new NotImplementedException();
                    default:
                        webRequest.Headers.Add(header.Key, header.Value);
                        break;
                }
            }
        }

        private CredentialCache GetCredentialCache()
        {
            return _credentialCache.Get("credentialCache", () => new CredentialCache());
        }
    }
}
