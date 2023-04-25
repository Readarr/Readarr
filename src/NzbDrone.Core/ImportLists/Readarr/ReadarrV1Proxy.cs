using System;
using System.Collections.Generic;
using System.Net;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.Readarr
{
    public interface IReadarrV1Proxy
    {
        List<ReadarrAuthor> GetAuthors(ReadarrSettings settings);
        List<ReadarrBook> GetBooks(ReadarrSettings settings);
        List<ReadarrProfile> GetProfiles(ReadarrSettings settings);
        List<ReadarrRootFolder> GetRootFolders(ReadarrSettings settings);
        List<ReadarrTag> GetTags(ReadarrSettings settings);
        ValidationFailure Test(ReadarrSettings settings);
    }

    public class ReadarrV1Proxy : IReadarrV1Proxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public ReadarrV1Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public List<ReadarrAuthor> GetAuthors(ReadarrSettings settings)
        {
            return Execute<ReadarrAuthor>("/api/v1/author", settings);
        }

        public List<ReadarrBook> GetBooks(ReadarrSettings settings)
        {
            return Execute<ReadarrBook>("/api/v1/book", settings);
        }

        public List<ReadarrProfile> GetProfiles(ReadarrSettings settings)
        {
            return Execute<ReadarrProfile>("/api/v1/qualityprofile", settings);
        }

        public List<ReadarrRootFolder> GetRootFolders(ReadarrSettings settings)
        {
            return Execute<ReadarrRootFolder>("api/v1/rootfolder", settings);
        }

        public List<ReadarrTag> GetTags(ReadarrSettings settings)
        {
            return Execute<ReadarrTag>("/api/v1/tag", settings);
        }

        public ValidationFailure Test(ReadarrSettings settings)
        {
            try
            {
                GetAuthors(settings);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.Error(ex, "API Key is invalid");
                    return new ValidationFailure("ApiKey", "API Key is invalid");
                }

                _logger.Error(ex, "Unable to send test message");
                return new ValidationFailure("ApiKey", "Unable to send test message");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to send test message");
                return new ValidationFailure("", "Unable to send test message");
            }

            return null;
        }

        private List<TResource> Execute<TResource>(string resource, ReadarrSettings settings)
        {
            if (settings.BaseUrl.IsNullOrWhiteSpace() || settings.ApiKey.IsNullOrWhiteSpace())
            {
                return new List<TResource>();
            }

            var baseUrl = settings.BaseUrl.TrimEnd('/');

            var request = new HttpRequestBuilder(baseUrl).Resource(resource).Accept(HttpAccept.Json)
                .SetHeader("X-Api-Key", settings.ApiKey).Build();

            var response = _httpClient.Get(request);

            var results = JsonConvert.DeserializeObject<List<TResource>>(response.Content);

            return results;
        }
    }
}
