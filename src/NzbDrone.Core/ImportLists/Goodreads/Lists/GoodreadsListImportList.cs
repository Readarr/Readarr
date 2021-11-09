using System;
using System.Collections.Generic;
using System.Linq;
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
    public class GoodreadsListImportList : ImportListBase<GoodreadsListImportListSettings>
    {
        private readonly IProvideListInfo _listInfo;

        public override string Name => "Goodreads List";
        public override ImportListType ListType => ImportListType.Goodreads;

        public GoodreadsListImportList(IProvideListInfo listInfo,
            IImportListStatusService importListStatusService,
            IConfigService configService,
            IParsingService parsingService,
            Logger logger)
            : base(importListStatusService, configService, parsingService, logger)
        {
            _listInfo = listInfo;
        }

        public override IList<ImportListItemInfo> Fetch()
        {
            var result = new List<ImportListItemInfo>();

            try
            {
                var pageNum = 1;
                while (true)
                {
                    if (pageNum > 100)
                    {
                        // you always seem to get back page 100 for bigger pages...
                        break;
                    }

                    var page = FetchPage(pageNum++);

                    if (page.Any())
                    {
                        result.AddRange(page);
                    }
                    else
                    {
                        break;
                    }
                }

                _importListStatusService.RecordSuccess(Definition.Id);
            }
            catch
            {
                _importListStatusService.RecordFailure(Definition.Id);
            }

            return CleanupListItems(result);
        }

        private List<ImportListItemInfo> FetchPage(int page)
        {
            var list = _listInfo.GetListInfo(Settings.ListId, page);
            var result = new List<ImportListItemInfo>();

            foreach (var book in list.Books)
            {
                var author = book.Authors.FirstOrDefault();

                result.Add(new ImportListItemInfo
                {
                    BookGoodreadsId = book.Work.Id.ToString(),
                    Book = book.Work.OriginalTitle,
                    EditionGoodreadsId = book.Id.ToString(),
                    Author = author?.Name,
                    AuthorGoodreadsId = author?.Id.ToString()
                });
            }

            return result;
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                _listInfo.GetListInfo(Settings.ListId, 1);
                return null;
            }
            catch (HttpException e)
            {
                _logger.Warn(e, "Goodreads API Error");
                if (e.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new ValidationFailure(nameof(Settings.ListId), $"List {Settings.ListId} not found");
                }

                return new ValidationFailure(nameof(Settings.ListId), $"Could not get list data");
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to connect to Goodreads");

                return new ValidationFailure(string.Empty, "Unable to connect to import list, check the log for more details");
            }
        }
    }
}
