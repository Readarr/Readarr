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

                if (ex.Response.HasHttpRedirect)
                {
                    _logger.Error(ex, "Readarr returned redirect and is invalid");
                    return new ValidationFailure("BaseUrl", "Readarr URL is invalid, are you missing a URL base?");
                }

                _logger.Error(ex, "Unable to connect to import list.");
                return new ValidationFailure(string.Empty, $"Unable to connect to import list: {ex.Message}. Check the log surrounding this error for details.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to connect to import list.");
                return new ValidationFailure(string.Empty, $"Unable to connect to import list: {ex.Message}. Check the log surrounding this error for details.");
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

            var request = new HttpRequestBuilder(baseUrl).Resource(resource)
                .Accept(HttpAccept.Json)
                .SetHeader("X-Api-Key", settings.ApiKey)
                .Build();

            var response = _httpClient.Get(request);

            if ((int)response.StatusCode >= 300)
            {
                throw new HttpException(response);
            }

            var results = JsonConvert.DeserializeObject<List<TResource>>(response.Content);

            return results;
        }
    }
}
