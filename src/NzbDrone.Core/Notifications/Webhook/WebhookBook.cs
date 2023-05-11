using System;
using System.Linq;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookBook
    {
        public WebhookBook()
        {
        }

        public WebhookBook(Book book)
        {
            Id = book.Id;
            GoodreadsId = book.ForeignBookId;
            Title = book.Title;
            ReleaseDate = book.ReleaseDate;
            Edition = new WebhookBookEdition(book.Editions.Value.Single(e => e.Monitored));
        }

        public int Id { get; set; }
        public string GoodreadsId { get; set; }
        public string Title { get; set; }
        public WebhookBookEdition Edition { get; set; }
        public DateTime? ReleaseDate { get; set; }
    }
}
