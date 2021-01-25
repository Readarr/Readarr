using System.Collections.Generic;

namespace NzbDrone.Core.Indexers.Newznab
{
    public class NewznabCapabilities
    {
        public int DefaultPageSize { get; set; }
        public int MaxPageSize { get; set; }
        public string[] SupportedSearchParameters { get; set; }
        public string[] SupportedAudioSearchParameters { get; set; }
        public string[] SupportedBookSearchParameters { get; set; }
        public bool SupportsAggregateIdSearch { get; set; }
        public string TextSearchEngine { get; set; }
        public string AudioTextSearchEngine { get; set; }
        public string BookTextSearchEngine { get; set; }
        public List<NewznabCategory> Categories { get; set; }

        public NewznabCapabilities()
        {
            DefaultPageSize = 100;
            MaxPageSize = 100;
            SupportedSearchParameters = new[] { "q" };
            SupportedAudioSearchParameters = new[] { "q", "artist", "album" };
            SupportedBookSearchParameters = new[] { "q", "author", "title" };
            SupportsAggregateIdSearch = false;
            TextSearchEngine = "sphinx"; // This should remain 'sphinx' for older newznab installs
            AudioTextSearchEngine = "sphinx"; // This should remain 'sphinx' for older newznab installs
            BookTextSearchEngine = "sphinx"; // This should remain 'sphinx' for older newznab installs
            Categories = new List<NewznabCategory>();
        }
    }

    public class NewznabCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public List<NewznabCategory> Subcategories { get; set; }
    }
}
