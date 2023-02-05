namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRetagPayload : WebhookPayload
    {
        public WebhookAuthor Author { get; set; }
        public WebhookBookFile BookFile { get; set; }
    }
}
