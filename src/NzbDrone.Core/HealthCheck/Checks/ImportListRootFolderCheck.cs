using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Books.Events;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.Localization;
using NzbDrone.Core.MediaFiles.Events;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(AuthorDeletedEvent))]
    [CheckOn(typeof(AuthorMovedEvent))]
    [CheckOn(typeof(BookImportedEvent), CheckOnCondition.FailedOnly)]
    [CheckOn(typeof(TrackImportedEvent), CheckOnCondition.FailedOnly)]
    [CheckOn(typeof(TrackImportFailedEvent), CheckOnCondition.SuccessfulOnly)]
    public class ImportListRootFolderCheck : HealthCheckBase
    {
        private readonly IImportListFactory _importListFactory;
        private readonly IDiskProvider _diskProvider;

        public ImportListRootFolderCheck(IImportListFactory importListFactory, IDiskProvider diskProvider, ILocalizationService localizationService)
            : base(localizationService)
        {
            _importListFactory = importListFactory;
            _diskProvider = diskProvider;
        }

        public override HealthCheck Check()
        {
            var importLists = _importListFactory.All();
            var missingRootFolders = new Dictionary<string, List<ImportListDefinition>>();

            foreach (var importList in importLists)
            {
                var rootFolderPath = importList.RootFolderPath;

                if (missingRootFolders.ContainsKey(rootFolderPath))
                {
                    missingRootFolders[rootFolderPath].Add(importList);

                    continue;
                }

                if (!_diskProvider.FolderExists(rootFolderPath))
                {
                    missingRootFolders.Add(rootFolderPath, new List<ImportListDefinition> { importList });
                }
            }

            if (missingRootFolders.Any())
            {
                if (missingRootFolders.Count == 1)
                {
                    var missingRootFolder = missingRootFolders.First();
                    return new HealthCheck(GetType(), HealthCheckResult.Error, string.Format(_localizationService.GetLocalizedString("ImportListMissingRoot"), FormatRootFolder(missingRootFolder.Key, missingRootFolder.Value)), "#import-list-missing-root-folder");
                }

                var message = string.Format(_localizationService.GetLocalizedString("ImportListMultipleMissingRoots"), string.Join(" | ", missingRootFolders.Select(m => FormatRootFolder(m.Key, m.Value))));
                return new HealthCheck(GetType(), HealthCheckResult.Error, message, "#import_list_missing_root_folder");
            }

            return new HealthCheck(GetType());
        }

        private string FormatRootFolder(string rootFolderPath, List<ImportListDefinition> importLists)
        {
            return $"{rootFolderPath} ({string.Join(", ", importLists.Select(l => l.Name))})";
        }
    }
}
