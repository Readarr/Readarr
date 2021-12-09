using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Goodreads
{
    public class GoodreadsBookshelf : GoodreadsNotificationBase<GoodreadsBookshelfNotificationSettings>
    {
        public GoodreadsBookshelf(IHttpClient httpClient,
                              Logger logger)
        : base(httpClient, logger)
        {
        }

        public override string Name => "Goodreads Bookshelves";
        public override string Link => "https://goodreads.com/";

        public override void OnReleaseImport(BookDownloadMessage message)
        {
            var importedBook = message.Book;

            foreach (var shelf in Settings.RemoveIds)
            {
                // try to find the edition that we need to remove
                var listBooks = SearchShelf(shelf, importedBook.AuthorMetadata.Value.Name);
                var toRemove = listBooks.Where(x => x.Book.WorkId.ToString() == importedBook.ForeignBookId);

                foreach (var listBook in toRemove)
                {
                    RemoveBookFromShelves(listBook.Book.Id, shelf);
                }
            }

            var bookId = importedBook.Editions.Value.Single(x => x.Monitored).ForeignEditionId;
            AddToShelves(bookId, Settings.AddIds);
        }

        public override void OnAuthorDelete(AuthorDeleteMessage deleteMessage)
        {
            if (deleteMessage.DeletedFiles)
            {
                foreach (var shelf in Settings.RemoveIds)
                {
                    var listBooks = SearchShelf(shelf, deleteMessage.Author.Name);
                    var toRemove = listBooks.Where(x => deleteMessage.Author.Books.Value.Select(b => b.ForeignBookId).Contains(x.Book.WorkId.ToString()));

                    foreach (var listBook in toRemove)
                    {
                        RemoveBookFromShelves(listBook.Book.Id, shelf);
                    }
                }
            }
        }

        public override void OnBookDelete(BookDeleteMessage deleteMessage)
        {
            if (deleteMessage.DeletedFiles)
            {
                foreach (var shelf in Settings.RemoveIds)
                {
                    var listBooks = SearchShelf(shelf, deleteMessage.Book.Author.Value.Name);
                    var toRemove = listBooks.Where(x => x.Book.WorkId.ToString() == deleteMessage.Book.ForeignBookId);

                    foreach (var listBook in toRemove)
                    {
                        RemoveBookFromShelves(listBook.Book.Id, shelf);
                    }
                }
            }
        }

        public override void OnBookFileDelete(BookFileDeleteMessage deleteMessage)
        {
            foreach (var shelf in Settings.RemoveIds)
            {
                var listBooks = SearchShelf(shelf, deleteMessage.Book.Author.Value.Name);
                var toRemove = listBooks.Where(x => x.Book.WorkId.ToString() == deleteMessage.Book.ForeignBookId);

                foreach (var listBook in toRemove)
                {
                    RemoveBookFromShelves(listBook.Book.Id, shelf);
                }
            }
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "getBookshelves")
            {
                if (Settings.AccessToken.IsNullOrWhiteSpace())
                {
                    return new
                    {
                        shelves = new List<object>()
                    };
                }

                Settings.Validate().Filter("AccessToken").ThrowOnError();

                var shelves = new List<UserShelfResource>();
                var page = 0;

                while (true)
                {
                    var curr = GetShelfList(++page);
                    if (curr == null || curr.Count == 0)
                    {
                        break;
                    }

                    shelves.AddRange(curr);
                }

                _logger.Trace($"Name: {query["name"]} {query["name"] == "removeIds"}");

                var helptext = new
                {
                    addIds = $"Add imported book to {Settings.UserName}'s shelves:",
                    removeIds = $"Remove imported book from {Settings.UserName}'s shelves:"
                };

                return new
                {
                    options = new
                    {
                        helptext,
                        user = Settings.UserName,
                        shelves = shelves.OrderBy(p => p.Name)
                        .Select(p => new
                        {
                            id = p.Name,
                            name = p.Name
                        })
                    }
                };
            }
            else
            {
                return base.RequestAction(action, query);
            }
        }

        private IReadOnlyList<UserShelfResource> GetShelfList(int page)
        {
            try
            {
                var builder = RequestBuilder()
                    .SetSegment("route", $"shelf/list.xml")
                    .AddQueryParam("user_id", Settings.UserId)
                    .AddQueryParam("page", page);

                var httpResponse = OAuthExecute(builder);

                return httpResponse.Deserialize<PaginatedList<UserShelfResource>>("shelves").List;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Error fetching bookshelves from Goodreads");
                return new List<UserShelfResource>();
            }
        }

        private IReadOnlyList<ReviewResource> SearchShelf(string shelf, string query)
        {
            List<ReviewResource> results = new ();

            while (true)
            {
                var page = 1;

                try
                {
                    var builder = RequestBuilder()
                        .SetSegment("route", $"review/list.xml")
                        .AddQueryParam("v", 2)
                        .AddQueryParam("id", Settings.UserId)
                        .AddQueryParam("shelf", shelf)
                        .AddQueryParam("per_page", 200)
                        .AddQueryParam("page", page++)
                        .AddQueryParam("search[query]", query);

                    var httpResponse = OAuthExecute(builder);

                    var resource = httpResponse.Deserialize<PaginatedList<ReviewResource>>("reviews");

                    results.AddRange(resource.List);

                    if (resource.Pagination.End >= resource.Pagination.TotalItems)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Error fetching bookshelves from Goodreads");
                    return results;
                }
            }

            return results;
        }

        private void RemoveBookFromShelves(long bookId, string shelf)
        {
            var req = RequestBuilder()
                .Post()
                .SetSegment("route", "shelf/add_to_shelf.xml")
                .AddFormParameter("name", shelf)
                .AddFormParameter("book_id", bookId)
                .AddFormParameter("a", "remove");

            // in case not found in shelf
            req.SuppressHttpError = true;

            OAuthExecute(req);
        }

        private void AddToShelves(string bookId, IEnumerable<string> shelves)
        {
            var req = RequestBuilder()
                .Post()
                .SetSegment("route", "shelf/add_books_to_shelves.xml")
                .AddFormParameter("bookids", bookId)
                .AddFormParameter("shelves", shelves.ConcatToString());

            OAuthExecute(req);
        }
    }
}
