using System;
using System.Collections.Generic;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Qualities;
using Readarr.Api.V1.Author;
using Readarr.Api.V1.CustomFormats;
using Readarr.Http.REST;

namespace Readarr.Api.V1.Blocklist
{
    public class BlocklistResource : RestResource
    {
        public int AuthorId { get; set; }
        public List<int> BookIds { get; set; }
        public string SourceTitle { get; set; }
        public QualityModel Quality { get; set; }
        public List<CustomFormatResource> CustomFormats { get; set; }
        public DateTime Date { get; set; }
        public DownloadProtocol Protocol { get; set; }
        public string Indexer { get; set; }
        public string Message { get; set; }

        public AuthorResource Author { get; set; }
    }

    public static class BlocklistResourceMapper
    {
        public static BlocklistResource MapToResource(this NzbDrone.Core.Blocklisting.Blocklist model, ICustomFormatCalculationService formatCalculator)
        {
            if (model == null)
            {
                return null;
            }

            return new BlocklistResource
            {
                Id = model.Id,

                AuthorId = model.AuthorId,
                BookIds = model.BookIds,
                SourceTitle = model.SourceTitle,
                Quality = model.Quality,
                CustomFormats = formatCalculator.ParseCustomFormat(model, model.Author).ToResource(false),
                Date = model.Date,
                Protocol = model.Protocol,
                Indexer = model.Indexer,
                Message = model.Message,

                Author = model.Author.ToResource()
            };
        }
    }
}
