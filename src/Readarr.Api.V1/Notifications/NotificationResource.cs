using NzbDrone.Core.Notifications;

namespace Readarr.Api.V1.Notifications
{
    public class NotificationResource : ProviderResource<NotificationResource>
    {
        public string Link { get; set; }
        public bool OnGrab { get; set; }
        public bool OnReleaseImport { get; set; }
        public bool OnUpgrade { get; set; }
        public bool OnRename { get; set; }
        public bool OnAuthorDelete { get; set; }
        public bool OnBookDelete { get; set; }
        public bool OnBookFileDelete { get; set; }
        public bool OnBookFileDeleteForUpgrade { get; set; }
        public bool OnHealthIssue { get; set; }
        public bool OnDownloadFailure { get; set; }
        public bool OnImportFailure { get; set; }
        public bool OnBookRetag { get; set; }
        public bool OnApplicationUpdate { get; set; }
        public bool SupportsOnGrab { get; set; }
        public bool SupportsOnReleaseImport { get; set; }
        public bool SupportsOnUpgrade { get; set; }
        public bool SupportsOnRename { get; set; }
        public bool SupportsOnAuthorDelete { get; set; }
        public bool SupportsOnBookDelete { get; set; }
        public bool SupportsOnBookFileDelete { get; set; }
        public bool SupportsOnBookFileDeleteForUpgrade { get; set; }
        public bool SupportsOnHealthIssue { get; set; }
        public bool IncludeHealthWarnings { get; set; }
        public bool SupportsOnDownloadFailure { get; set; }
        public bool SupportsOnImportFailure { get; set; }
        public bool SupportsOnBookRetag { get; set; }
        public bool SupportsOnApplicationUpdate { get; set; }
        public string TestCommand { get; set; }
    }

    public class NotificationResourceMapper : ProviderResourceMapper<NotificationResource, NotificationDefinition>
    {
        public override NotificationResource ToResource(NotificationDefinition definition)
        {
            if (definition == null)
            {
                return default(NotificationResource);
            }

            var resource = base.ToResource(definition);

            resource.OnGrab = definition.OnGrab;
            resource.OnReleaseImport = definition.OnReleaseImport;
            resource.OnUpgrade = definition.OnUpgrade;
            resource.OnRename = definition.OnRename;
            resource.OnAuthorDelete = definition.OnAuthorDelete;
            resource.OnBookDelete = definition.OnBookDelete;
            resource.OnBookFileDelete = definition.OnBookFileDelete;
            resource.OnBookFileDeleteForUpgrade = definition.OnBookFileDeleteForUpgrade;
            resource.OnHealthIssue = definition.OnHealthIssue;
            resource.OnDownloadFailure = definition.OnDownloadFailure;
            resource.OnImportFailure = definition.OnImportFailure;
            resource.OnBookRetag = definition.OnBookRetag;
            resource.OnApplicationUpdate = definition.OnApplicationUpdate;
            resource.SupportsOnGrab = definition.SupportsOnGrab;
            resource.SupportsOnReleaseImport = definition.SupportsOnReleaseImport;
            resource.SupportsOnUpgrade = definition.SupportsOnUpgrade;
            resource.SupportsOnRename = definition.SupportsOnRename;
            resource.SupportsOnAuthorDelete = definition.SupportsOnAuthorDelete;
            resource.SupportsOnBookDelete = definition.SupportsOnBookDelete;
            resource.SupportsOnBookFileDelete = definition.SupportsOnBookFileDelete;
            resource.SupportsOnBookFileDeleteForUpgrade = definition.SupportsOnBookFileDeleteForUpgrade;
            resource.SupportsOnHealthIssue = definition.SupportsOnHealthIssue;
            resource.IncludeHealthWarnings = definition.IncludeHealthWarnings;
            resource.SupportsOnDownloadFailure = definition.SupportsOnDownloadFailure;
            resource.SupportsOnImportFailure = definition.SupportsOnImportFailure;
            resource.SupportsOnBookRetag = definition.SupportsOnBookRetag;
            resource.SupportsOnApplicationUpdate = definition.SupportsOnApplicationUpdate;

            return resource;
        }

        public override NotificationDefinition ToModel(NotificationResource resource)
        {
            if (resource == null)
            {
                return default(NotificationDefinition);
            }

            var definition = base.ToModel(resource);

            definition.OnGrab = resource.OnGrab;
            definition.OnReleaseImport = resource.OnReleaseImport;
            definition.OnUpgrade = resource.OnUpgrade;
            definition.OnRename = resource.OnRename;
            definition.OnAuthorDelete = resource.OnAuthorDelete;
            definition.OnBookDelete = resource.OnBookDelete;
            definition.OnBookFileDelete = resource.OnBookFileDelete;
            definition.OnBookFileDeleteForUpgrade = resource.OnBookFileDeleteForUpgrade;
            definition.OnHealthIssue = resource.OnHealthIssue;
            definition.OnDownloadFailure = resource.OnDownloadFailure;
            definition.OnImportFailure = resource.OnImportFailure;
            definition.OnBookRetag = resource.OnBookRetag;
            definition.OnApplicationUpdate = resource.OnApplicationUpdate;
            definition.SupportsOnGrab = resource.SupportsOnGrab;
            definition.SupportsOnReleaseImport = resource.SupportsOnReleaseImport;
            definition.SupportsOnUpgrade = resource.SupportsOnUpgrade;
            definition.SupportsOnRename = resource.SupportsOnRename;
            definition.SupportsOnAuthorDelete = resource.SupportsOnAuthorDelete;
            definition.SupportsOnBookDelete = resource.SupportsOnBookDelete;
            definition.SupportsOnBookFileDelete = resource.SupportsOnBookFileDelete;
            definition.SupportsOnBookFileDeleteForUpgrade = resource.SupportsOnBookFileDeleteForUpgrade;
            definition.SupportsOnHealthIssue = resource.SupportsOnHealthIssue;
            definition.IncludeHealthWarnings = resource.IncludeHealthWarnings;
            definition.SupportsOnDownloadFailure = resource.SupportsOnDownloadFailure;
            definition.SupportsOnImportFailure = resource.SupportsOnImportFailure;
            definition.SupportsOnBookRetag = resource.SupportsOnBookRetag;
            definition.SupportsOnApplicationUpdate = resource.SupportsOnApplicationUpdate;

            return definition;
        }
    }
}
