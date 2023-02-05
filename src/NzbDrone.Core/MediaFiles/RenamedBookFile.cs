namespace NzbDrone.Core.MediaFiles
{
    public class RenamedBookFile
    {
        public BookFile BookFile { get; set; }
        public string PreviousPath { get; set; }
    }
}
