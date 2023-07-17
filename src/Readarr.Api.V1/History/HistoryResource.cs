using System;
using System.Collections.Generic;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.History;
using NzbDrone.Core.Qualities;
using Readarr.Api.V1.Author;
using Readarr.Api.V1.Books;
using Readarr.Api.V1.CustomFormats;
using Readarr.Http.REST;

namespace Readarr.Api.V1.History
{
    public class HistoryResource : RestResource
    {
        public int BookId { get; set; }
        public int AuthorId { get; set; }
        public string SourceTitle { get; set; }
        public QualityModel Quality { get; set; }
        public List<CustomFormatResource> CustomFormats { get; set; }
        public int CustomFormatScore { get; set; }
        public bool QualityCutoffNotMet { get; set; }
        public DateTime Date { get; set; }
        public string DownloadId { get; set; }

        public EntityHistoryEventType EventType { get; set; }

        public Dictionary<string, string> Data { get; set; }

        public BookResource Book { get; set; }
        public AuthorResource Author { get; set; }
    }

    public static class HistoryResourceMapper
    {
        public static HistoryResource ToResource(this EntityHistory model, ICustomFormatCalculationService formatCalculator)
        {
            if (model == null)
            {
                return null;
            }

            var customFormats = formatCalculator.ParseCustomFormat(model, model.Author);
            var customFormatScore = model.Author?.QualityProfile?.Value?.CalculateCustomFormatScore(customFormats) ?? 0;

            return new HistoryResource
            {
                Id = model.Id,

                BookId = model.BookId,
                AuthorId = model.AuthorId,
                SourceTitle = model.SourceTitle,
                Quality = model.Quality,
                CustomFormats = customFormats.ToResource(false),
                CustomFormatScore = customFormatScore,

                //QualityCutoffNotMet
                Date = model.Date,
                DownloadId = model.DownloadId,

                EventType = model.EventType,

                Data = model.Data

                //Episode
                //Series
            };
        }
    }
}
