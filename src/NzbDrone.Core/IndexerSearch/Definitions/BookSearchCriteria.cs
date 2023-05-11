using NzbDrone.Core.Parser;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class BookSearchCriteria : SearchCriteriaBase
    {
        public string BookTitle { get; set; }
        public int BookYear { get; set; }
        public string BookIsbn { get; set; }
        public string Disambiguation { get; set; }

        public string BookQuery => GetQueryTitle(BookTitle.SplitBookTitle(Author.Name).Item1);

        public override string ToString()
        {
            return $"[{Author.Name} - {BookTitle}]";
        }
    }
}
