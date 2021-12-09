using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace NzbDrone.Core.Books.Calibre
{
    public class CalibreBook
    {
        [JsonProperty("application_id")]
        public int Id { get; set; }

        public string Title { get; set; }

        public List<string> Authors { get; set; }

        [JsonProperty("author_sort")]
        public string AuthorSort { get; set; }

        [JsonConverter(typeof(CalibreDateConverter))]
        public DateTime? PubDate { get; set; }

        public string Publisher { get; set; }

        public List<string> Languages { get; set; }

        public List<string> Tags { get; set; }

        public string Comments { get; set; }

        public double Rating { get; set; }

        public Dictionary<string, string> Identifiers { get; set; }

        public string Series { get; set; }

        [JsonProperty("series_index")]
        public double? Position { get; set; }

        [JsonProperty("format_metadata")]
        public Dictionary<string, CalibreBookFormat> Formats { get; set; }

        public Dictionary<string, Tuple<string, string>> Diff(CalibreBook other)
        {
            var output = new Dictionary<string, Tuple<string, string>>();

            if (Title != other.Title)
            {
                output.Add("Title", Tuple.Create(Title, other.Title));
            }

            if (!Authors.SequenceEqual(other.Authors))
            {
                var oldValue = Authors.Any() ? string.Join(" / ", Authors) : null;
                var newValue = other.Authors.Any() ? string.Join(" / ", other.Authors) : null;

                output.Add("Author", Tuple.Create(oldValue, newValue));
            }

            var oldDate = PubDate.HasValue ? PubDate.Value.ToString("MMM-yyyy") : null;
            var newDate = other.PubDate.HasValue ? other.PubDate.Value.ToString("MMM-yyyy") : null;
            if (oldDate != newDate)
            {
                output.Add("PubDate", Tuple.Create(oldDate, newDate));
            }

            if (Publisher != other.Publisher)
            {
                output.Add("Publisher", Tuple.Create(Publisher, other.Publisher));
            }

            if (!Languages.OrderBy(x => x).SequenceEqual(other.Languages.OrderBy(x => x)))
            {
                output.Add("Languages", Tuple.Create(string.Join(" / ", Languages), string.Join(" / ", other.Languages)));
            }

            if (!Tags.OrderBy(x => x).SequenceEqual(other.Tags.OrderBy(x => x)))
            {
                output.Add("Tags", Tuple.Create(string.Join(" / ", Tags), string.Join(" / ", other.Tags)));
            }

            if (Comments != other.Comments)
            {
                output.Add("Comments", Tuple.Create(Comments, other.Comments));
            }

            if (Rating != other.Rating)
            {
                output.Add("Rating", Tuple.Create(Rating.ToString(), other.Rating.ToString()));
            }

            if (!Identifiers.Where(x => x.Value != null).OrderBy(x => x.Key).SequenceEqual(
                other.Identifiers.Where(x => x.Value != null).OrderBy(x => x.Key)))
            {
                output.Add("Identifiers", Tuple.Create(
                    string.Join(" / ", Identifiers.Where(x => x.Value != null).OrderBy(x => x.Key)),
                    string.Join(" / ", other.Identifiers.Where(x => x.Value != null).OrderBy(x => x.Key))));
            }

            if (Series != other.Series)
            {
                output.Add("Series", Tuple.Create(Series, other.Series));
            }

            if (Position != other.Position)
            {
                output.Add("Series Index", Tuple.Create(Position.ToString(), other.Position.ToString()));
            }

            return output;
        }
    }

    public class CalibreBookFormat
    {
        public string Path { get; set; }

        public long Size { get; set; }

        [JsonProperty("mtime")]
        public DateTime LastModified { get; set; }
    }
}
