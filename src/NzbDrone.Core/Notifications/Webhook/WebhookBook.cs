using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookBook
    {
        public WebhookBook()
        {
            Editions = new List<WebhookBookEdition>();
        }

        public WebhookBook(Book book)
        {
            Id = book.Id;
            GoodreadsId = book.ForeignBookId;
            Title = book.Title;
            ReleaseDate = book.ReleaseDate;
            Editions = book.Editions.Value.Select(x => new WebhookBookEdition(x)).ToList();
        }

        public int Id { get; set; }
        public string GoodreadsId { get; set; }
        public string Title { get; set; }
        public List<WebhookBookEdition> Editions { get; set; }
        public DateTime? ReleaseDate { get; set; }
    }
}
