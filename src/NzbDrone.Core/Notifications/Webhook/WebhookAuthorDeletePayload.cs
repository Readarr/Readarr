namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookAuthorDeletePayload : WebhookPayload
    {
        public WebhookAuthor Author { get; set; }
        public bool DeletedFiles { get; set; }
    }
}
