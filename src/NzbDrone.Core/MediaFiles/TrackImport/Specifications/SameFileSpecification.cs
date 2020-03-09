using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.TrackImport.Specifications
{
    public class SameFileSpecification : IImportDecisionEngineSpecification<LocalTrack>
    {
        private readonly Logger _logger;

        public SameFileSpecification(Logger logger)
        {
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalTrack item, DownloadClientItem downloadClientItem)
        {
            if (item.Album.BookFileId == 0)
            {
                _logger.Debug("No existing track file, skipping");
                return Decision.Accept();
            }

            var trackFile = item.Album?.BookFile?.Value;

            if (trackFile == null)
            {
                _logger.Debug("No existing track file, skipping");
                return Decision.Accept();
            }

            if (trackFile.Size == item.Size)
            {
                _logger.Debug("'{0}' Has the same filesize as existing file", item.Path);
                return Decision.Reject("Has the same filesize as existing file");
            }

            return Decision.Accept();
        }
    }
}
