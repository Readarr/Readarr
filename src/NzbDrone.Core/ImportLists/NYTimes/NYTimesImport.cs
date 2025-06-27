using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.ImportLists.NYTimes
{
    public class NYTimesImport : ImportListBase<NYTimesSettings>
    {
        protected readonly IHttpClient _httpClient;

        public override string Name => "New York Times";

        public override ImportListType ListType => ImportListType.Other;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);

        public NYTimesImport(IImportListStatusService importListStatusService,
            IConfigService configService,
            IParsingService parsingService,
            IHttpClient httpClient,
            Logger logger)
        : base(importListStatusService, configService, parsingService, logger)
        {
            _httpClient = httpClient;
        }

        public List<NYTimesName> GetNames(NYTimesSettings settings)
        {
            if (settings.ApiKey.IsNullOrWhiteSpace())
            {
                return new List<NYTimesName>();
            }

            var request = new HttpRequestBuilder(settings.BaseUrl)
                .Resource("/lists/names.json")
                .AddQueryParam("api-key", settings.ApiKey)
                .Build();

            var response = _httpClient.Get(request);

            var content = JsonConvert.DeserializeObject<NYTimesResponse<List<NYTimesName>>>(response.Content);

            return content.Results;
        }

        public List<NYTimesList> GetList(NYTimesSettings settings)
        {
            if (settings.ApiKey.IsNullOrWhiteSpace() || settings.ListName.IsNullOrWhiteSpace())
            {
                return new List<NYTimesList>();
            }

            var request = new HttpRequestBuilder(settings.BaseUrl)
                .Resource("/lists.json")
                .AddQueryParam("api-key", settings.ApiKey)
                .AddQueryParam("list", settings.ListName)
                .Build();

            var response = _httpClient.Get(request);

            var content = JsonConvert.DeserializeObject<NYTimesResponse<List<NYTimesList>>>(response.Content);

            return content.Results;
        }

        public override IList<ImportListItemInfo> Fetch()
        {
            var books = new List<ImportListItemInfo>();

            try
            {
                var lists = GetList(Settings);

                foreach (var list in lists)
                {
                    var book = list.BookDetails.FirstOrDefault();

                    books.Add(new ImportListItemInfo
                    {
                        Author = book.Author,
                        Book = book.Title
                    });
                }

                _importListStatusService.RecordSuccess(Definition.Id);
            }
            catch
            {
                _logger.Warn("List Import Sync Task Failed for List [{0}]", Definition.Name);
                _importListStatusService.RecordFailure(Definition.Id);
            }

            return CleanupListItems(books);
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                GetNames(Settings);
                return null;
            }
            catch (HttpException ex)
            {
                _logger.Warn(ex, "New York Times Authentication Error");
                return new ValidationFailure(string.Empty, $"NYTimes authentication error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to connect to NYTimes");

                return new ValidationFailure(string.Empty, "Unable to connect to import list, check the log for more details");
            }
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "getNames")
            {
                var names = GetNames(Settings);

                return new
                {
                    options = names.Select(name => new
                        {
                            Value = name.ListNameEncoded,
                            Name = name.DisplayName
                        })
                };
            }

            return new { };
        }
    }
}
