using NzbDrone.Core.Books;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookAuthor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string GoodreadsId { get; set; }

        public WebhookAuthor()
        {
        }

        public WebhookAuthor(Author author)
        {
            Id = author.Id;
            Name = author.Name;
            Path = author.Path;
            GoodreadsId = author.Metadata.Value.ForeignAuthorId;
        }
    }
}
