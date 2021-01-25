using System.Collections.Generic;
using System.Text.RegularExpressions;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public abstract class SearchCriteriaBase
    {
        private static readonly Regex NonWord = new Regex(@"[^\w']+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex BeginningThe = new Regex(@"^the\s", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex StandardizeSingleQuotesRegex = new Regex(@"[\u0060\u00B4\u2018\u2019]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public virtual bool MonitoredBooksOnly { get; set; }
        public virtual bool UserInvokedSearch { get; set; }
        public virtual bool InteractiveSearch { get; set; }

        public Author Author { get; set; }
        public List<Book> Books { get; set; }

        public string AuthorQuery => Author?.Name;
        public string CleanAuthorQuery => GetQueryTitle(AuthorQuery);

        public static string GetQueryTitle(string title)
        {
            Ensure.That(title, () => title).IsNotNullOrWhiteSpace();

            // Most VA books are listed as VA, not Various Authors
            // TODO: Needed in Readarr??
            if (title == "Various Authors")
            {
                title = "VA";
            }

            var cleanTitle = BeginningThe.Replace(title, string.Empty);
            cleanTitle = StandardizeSingleQuotesRegex.Replace(cleanTitle, "'");
            cleanTitle = NonWord.Replace(cleanTitle, "+");

            // remove any repeating +s
            cleanTitle = Regex.Replace(cleanTitle, @"\+{2,}", "+");
            cleanTitle = cleanTitle.RemoveAccent();
            cleanTitle = cleanTitle.Trim('+', ' ');

            return cleanTitle.Length == 0 ? title : cleanTitle;
        }
    }
}
