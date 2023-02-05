namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookBookDeletePayload : WebhookPayload
    {
        public WebhookAuthor Author { get; set; }
        public WebhookBook Book { get; set; }
        public bool DeletedFiles { get; set; }
    }
}
