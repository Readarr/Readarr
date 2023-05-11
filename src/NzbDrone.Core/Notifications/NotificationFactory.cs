using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public interface INotificationFactory : IProviderFactory<INotification, NotificationDefinition>
    {
        List<INotification> OnGrabEnabled();
        List<INotification> OnReleaseImportEnabled();
        List<INotification> OnUpgradeEnabled();
        List<INotification> OnRenameEnabled();
        List<INotification> OnHealthIssueEnabled();
        List<INotification> OnAuthorDeleteEnabled();
        List<INotification> OnBookDeleteEnabled();
        List<INotification> OnBookFileDeleteEnabled();
        List<INotification> OnBookFileDeleteForUpgradeEnabled();
        List<INotification> OnDownloadFailureEnabled();
        List<INotification> OnImportFailureEnabled();
        List<INotification> OnBookRetagEnabled();
        List<INotification> OnApplicationUpdateEnabled();
    }

    public class NotificationFactory : ProviderFactory<INotification, NotificationDefinition>, INotificationFactory
    {
        public NotificationFactory(INotificationRepository providerRepository, IEnumerable<INotification> providers, IServiceProvider container, IEventAggregator eventAggregator, Logger logger)
            : base(providerRepository, providers, container, eventAggregator, logger)
        {
        }

        public List<INotification> OnGrabEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnGrab).ToList();
        }

        public List<INotification> OnReleaseImportEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnReleaseImport).ToList();
        }

        public List<INotification> OnUpgradeEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnUpgrade).ToList();
        }

        public List<INotification> OnRenameEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnRename).ToList();
        }

        public List<INotification> OnAuthorDeleteEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnAuthorDelete).ToList();
        }

        public List<INotification> OnBookDeleteEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnBookDelete).ToList();
        }

        public List<INotification> OnBookFileDeleteEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnBookFileDelete).ToList();
        }

        public List<INotification> OnBookFileDeleteForUpgradeEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnBookFileDeleteForUpgrade).ToList();
        }

        public List<INotification> OnHealthIssueEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnHealthIssue).ToList();
        }

        public List<INotification> OnDownloadFailureEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnDownloadFailure).ToList();
        }

        public List<INotification> OnImportFailureEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnImportFailure).ToList();
        }

        public List<INotification> OnBookRetagEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnBookRetag).ToList();
        }

        public List<INotification> OnApplicationUpdateEnabled()
        {
            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnApplicationUpdate).ToList();
        }

        public override void SetProviderCharacteristics(INotification provider, NotificationDefinition definition)
        {
            base.SetProviderCharacteristics(provider, definition);

            definition.SupportsOnGrab = provider.SupportsOnGrab;
            definition.SupportsOnReleaseImport = provider.SupportsOnReleaseImport;
            definition.SupportsOnUpgrade = provider.SupportsOnUpgrade;
            definition.SupportsOnRename = provider.SupportsOnRename;
            definition.SupportsOnAuthorDelete = provider.SupportsOnAuthorDelete;
            definition.SupportsOnBookDelete = provider.SupportsOnBookDelete;
            definition.SupportsOnBookFileDelete = provider.SupportsOnBookFileDelete;
            definition.SupportsOnBookFileDeleteForUpgrade = provider.SupportsOnBookFileDeleteForUpgrade;
            definition.SupportsOnHealthIssue = provider.SupportsOnHealthIssue;
            definition.SupportsOnDownloadFailure = provider.SupportsOnDownloadFailure;
            definition.SupportsOnImportFailure = provider.SupportsOnImportFailure;
            definition.SupportsOnBookRetag = provider.SupportsOnBookRetag;
            definition.SupportsOnApplicationUpdate = provider.SupportsOnApplicationUpdate;
        }
    }
}
