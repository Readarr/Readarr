using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.Readarr
{
    public class ReadarrImport : ImportListBase<ReadarrSettings>
    {
        private readonly IReadarrV1Proxy _readarrV1Proxy;
        public override string Name => "Readarr";

        public override ImportListType ListType => ImportListType.Program;

        public ReadarrImport(IReadarrV1Proxy readarrV1Proxy,
                            IImportListStatusService importListStatusService,
                            IConfigService configService,
                            IParsingService parsingService,
                            Logger logger)
            : base(importListStatusService, configService, parsingService, logger)
        {
            _readarrV1Proxy = readarrV1Proxy;
        }

        public override IList<ImportListItemInfo> Fetch()
        {
            var authorsAndBooks = new List<ImportListItemInfo>();

            /*try
            {
                var remoteAuthors = _readarrV1Proxy.GetAuthors(Settings);

                foreach (var remoteAuthor in remoteAuthors)
                {
                    if ((!Settings.ProfileIds.Any() || Settings.ProfileIds.Contains(remoteAuthor.QualityProfileId)) &&
                        (!Settings.TagIds.Any() || Settings.TagIds.Any(x => remoteAuthor.Tags.Any(y => y == x))) &&
                        remoteAuthor.Monitored)
                    {
                        authorsAndBooks.Add(new ImportListItemInfo
                        {
                            Author = remoteAuthor.AuthorName,
                            AuthorGoodreadsId = remoteAuthor.ForeignAuthorId
                        });
                    }
                }

                _importListStatusService.RecordSuccess(Definition.Id);
            }
            catch
            {
                _importListStatusService.RecordFailure(Definition.Id);
            }*/

            try
            {
                var remoteBooks = _readarrV1Proxy.GetBooks(Settings);

                foreach (var remoteBook in remoteBooks)
                {
                    if ((!Settings.ProfileIds.Any() || Settings.ProfileIds.Contains(remoteBook.Author.QualityProfileId)) &&
                        (!Settings.TagIds.Any() || Settings.TagIds.Any(x => remoteBook.Author.Tags.Any(y => y == x))) &&
                         remoteBook.Monitored && remoteBook.Author.Monitored)
                    {
                        authorsAndBooks.Add(new ImportListItemInfo
                        {
                            BookGoodreadsId = remoteBook.ForeignBookId,
                            Book = remoteBook.Title,
                            EditionGoodreadsId = remoteBook.Editions[0].ForeignEditionId,
                            Author = remoteBook.Author.AuthorName,
                            AuthorGoodreadsId = remoteBook.Author.ForeignAuthorId
                        });
                    }
                }

                _importListStatusService.RecordSuccess(Definition.Id);
            }
            catch
            {
                _importListStatusService.RecordFailure(Definition.Id);
            }

            return CleanupListItems(authorsAndBooks);
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            // Return early if there is not an API key
            if (Settings.ApiKey.IsNullOrWhiteSpace())
            {
                return new
                {
                    devices = new List<object>()
                };
            }

            Settings.Validate().Filter("ApiKey").ThrowOnError();

            if (action == "getProfiles")
            {
                var devices = _readarrV1Proxy.GetProfiles(Settings);

                return new
                {
                    options = devices.OrderBy(d => d.Name, StringComparer.InvariantCultureIgnoreCase)
                                            .Select(d => new
                                            {
                                                Value = d.Id,
                                                Name = d.Name
                                            })
                };
            }

            if (action == "getTags")
            {
                var devices = _readarrV1Proxy.GetTags(Settings);

                return new
                {
                    options = devices.OrderBy(d => d.Label, StringComparer.InvariantCultureIgnoreCase)
                                            .Select(d => new
                                            {
                                                Value = d.Id,
                                                Name = d.Label
                                            })
                };
            }

            return new { };
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(_readarrV1Proxy.Test(Settings));
        }
    }
}
