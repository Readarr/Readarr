using System.Collections.Generic;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRenamePayload : WebhookPayload
    {
        public WebhookAuthor Author { get; set; }
        public List<WebhookRenamedBookFile> RenamedBookFiles { get; set; }
    }
}
