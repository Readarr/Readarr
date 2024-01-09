using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Queue;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class QueueSpecification : IDecisionEngineSpecification
    {
        private readonly IQueueService _queueService;
        private readonly UpgradableSpecification _upgradableSpecification;
        private readonly ICustomFormatCalculationService _formatService;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public QueueSpecification(IQueueService queueService,
                                  UpgradableSpecification upgradableSpecification,
                                  ICustomFormatCalculationService formatService,
                                  IConfigService configService,
                                  Logger logger)
        {
            _queueService = queueService;
            _upgradableSpecification = upgradableSpecification;
            _formatService = formatService;
            _configService = configService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public Decision IsSatisfiedBy(RemoteBook subject, SearchCriteriaBase searchCriteria)
        {
            var queue = _queueService.GetQueue();
            var matchingBook = queue.Where(q => q.RemoteBook?.Author != null &&
                                                 q.RemoteBook.Author.Id == subject.Author.Id &&
                                                 q.RemoteBook.Books.Select(e => e.Id).Intersect(subject.Books.Select(e => e.Id)).Any())
                           .ToList();

            foreach (var queueItem in matchingBook)
            {
                var remoteBook = queueItem.RemoteBook;
                var qualityProfile = subject.Author.QualityProfile.Value;

                // To avoid a race make sure it's not FailedPending (failed awaiting removal/search).
                // Failed items (already searching for a replacement) won't be part of the queue since
                // it's a copy, of the tracked download, not a reference.
                if (queueItem.TrackedDownloadState == TrackedDownloadState.DownloadFailedPending)
                {
                    continue;
                }

                _logger.Debug("Checking if existing release in queue meets cutoff. Queued quality is: {0}", remoteBook.ParsedBookInfo.Quality);

                var queuedItemCustomFormats = _formatService.ParseCustomFormat(remoteBook, (long)queueItem.Size);

                if (!_upgradableSpecification.CutoffNotMet(qualityProfile,
                                                           new List<QualityModel> { remoteBook.ParsedBookInfo.Quality },
                                                           queuedItemCustomFormats,
                                                           subject.ParsedBookInfo.Quality))
                {
                    return Decision.Reject("Release in queue already meets cutoff: {0}", remoteBook.ParsedBookInfo.Quality);
                }

                _logger.Debug("Checking if release is higher quality than queued release. Queued: {0}", remoteBook.ParsedBookInfo.Quality);

                if (!_upgradableSpecification.IsUpgradable(qualityProfile,
                                                           remoteBook.ParsedBookInfo.Quality,
                                                           queuedItemCustomFormats,
                                                           subject.ParsedBookInfo.Quality,
                                                           subject.CustomFormats))
                {
                    return Decision.Reject("Release in queue is of equal or higher preference: {0}", remoteBook.ParsedBookInfo.Quality);
                }

                _logger.Debug("Checking if profiles allow upgrading. Queued: {0}", remoteBook.ParsedBookInfo.Quality);

                if (!_upgradableSpecification.IsUpgradeAllowed(qualityProfile,
                                                               remoteBook.ParsedBookInfo.Quality,
                                                               queuedItemCustomFormats,
                                                               subject.ParsedBookInfo.Quality,
                                                               subject.CustomFormats))
                {
                    return Decision.Reject("Another release is queued and the Quality profile does not allow upgrades");
                }

                if (_upgradableSpecification.IsRevisionUpgrade(remoteBook.ParsedBookInfo.Quality, subject.ParsedBookInfo.Quality))
                {
                    if (_configService.DownloadPropersAndRepacks == ProperDownloadTypes.DoNotUpgrade)
                    {
                        _logger.Debug("Auto downloading of propers is disabled");
                        return Decision.Reject("Proper downloading is disabled");
                    }
                }
            }

            return Decision.Accept();
        }
    }
}
