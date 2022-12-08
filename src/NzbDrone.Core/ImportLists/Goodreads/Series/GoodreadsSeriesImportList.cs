using System;
using System.Collections.Generic;
using System.Net;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Goodreads
{
    public class GoodreadsSeriesImportList : ImportListBase<GoodreadsSeriesImportListSettings>
    {
        private readonly IProvideSeriesInfo _seriesInfo;

        public override string Name => "Goodreads Series";
        public override ImportListType ListType => ImportListType.Goodreads;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);

        public GoodreadsSeriesImportList(IProvideSeriesInfo seriesInfo,
            IImportListStatusService importListStatusService,
            IConfigService configService,
            IParsingService parsingService,
            Logger logger)
            : base(importListStatusService, configService, parsingService, logger)
        {
            _seriesInfo = seriesInfo;
        }

        public override IList<ImportListItemInfo> Fetch()
        {
            var result = new List<ImportListItemInfo>();

            try
            {
                var series = _seriesInfo.GetSeriesInfo(Settings.SeriesId);

                foreach (var work in series.Works)
                {
                    result.Add(new ImportListItemInfo
                    {
                        BookGoodreadsId = work.Id.ToString(),
                        Book = work.OriginalTitle,
                        EditionGoodreadsId = work.BestBook.Id.ToString(),
                        Author = work.BestBook.AuthorName,
                        AuthorGoodreadsId = work.BestBook.AuthorId.ToString()
                    });
                }

                _importListStatusService.RecordSuccess(Definition.Id);
            }
            catch
            {
                _importListStatusService.RecordFailure(Definition.Id);
            }

            return CleanupListItems(result);
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                _seriesInfo.GetSeriesInfo(Settings.SeriesId);
                return null;
            }
            catch (HttpException e)
            {
                _logger.Warn(e, "Goodreads API Error");
                if (e.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new ValidationFailure(nameof(Settings.SeriesId), $"Series {Settings.SeriesId} not found");
                }

                return new ValidationFailure(nameof(Settings.SeriesId), $"Could not get series data");
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to connect to Goodreads");

                return new ValidationFailure(string.Empty, "Unable to connect to import list, check the log for more details");
            }
        }
    }
}
