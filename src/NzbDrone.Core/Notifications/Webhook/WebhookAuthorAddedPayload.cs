namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookAuthorAddedPayload : WebhookPayload
    {
        public WebhookAuthor Author { get; set; }
    }
}
