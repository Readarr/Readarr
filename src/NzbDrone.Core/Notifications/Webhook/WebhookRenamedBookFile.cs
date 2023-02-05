using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRenamedBookFile : WebhookBookFile
    {
        public WebhookRenamedBookFile(RenamedBookFile renamedMovie)
            : base(renamedMovie.BookFile)
        {
            PreviousPath = renamedMovie.PreviousPath;
        }

        public string PreviousPath { get; set; }
    }
}
