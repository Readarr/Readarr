using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.RootFolders
{
    public interface IRootFolderService
    {
        List<RootFolder> All();
        List<RootFolder> AllWithSpaceStats();
        RootFolder Add(RootFolder rootFolder);
        RootFolder Update(RootFolder rootFolder);
        void Remove(int id);
        RootFolder Get(int id);
        List<RootFolder> AllForTag(int tagId);
        RootFolder GetBestRootFolder(string path);
        string GetBestRootFolderPath(string path);
        string GetBestRootFolderPath(string path, List<RootFolder> allRootFolders);
    }

    public class RootFolderService : IRootFolderService, IHandle<ModelEvent<RemotePathMapping>>
    {
        private readonly IRootFolderRepository _rootFolderRepository;
        private readonly IDiskProvider _diskProvider;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        public RootFolderService(IRootFolderRepository rootFolderRepository,
                                 IDiskProvider diskProvider,
                                 IManageCommandQueue commandQueueManager,
                                 Logger logger)
        {
            _rootFolderRepository = rootFolderRepository;
            _diskProvider = diskProvider;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        public List<RootFolder> All()
        {
            var rootFolders = _rootFolderRepository.All().ToList();

            return rootFolders;
        }

        public List<RootFolder> AllWithSpaceStats()
        {
            var rootFolders = _rootFolderRepository.All().ToList();

            rootFolders.ForEach(folder =>
            {
                try
                {
                    if (folder.Path.IsPathValid(PathValidationType.CurrentOs))
                    {
                        GetDetails(folder);
                    }
                }

                //We don't want an exception to prevent the root folders from loading in the UI, so they can still be deleted
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unable to get free space and unmapped folders for root folder {0}", folder.Path);
                }
            });

            return rootFolders;
        }

        private void VerifyRootFolder(RootFolder rootFolder)
        {
            if (string.IsNullOrWhiteSpace(rootFolder.Path) || !Path.IsPathRooted(rootFolder.Path))
            {
                throw new ArgumentException("Invalid path");
            }

            if (!_diskProvider.FolderExists(rootFolder.Path))
            {
                throw new DirectoryNotFoundException("Can't add root directory that doesn't exist.");
            }

            if (!_diskProvider.FolderWritable(rootFolder.Path))
            {
                throw new UnauthorizedAccessException(string.Format("Root folder path '{0}' is not writable by user '{1}'", rootFolder.Path, Environment.UserName));
            }
        }

        public RootFolder Add(RootFolder rootFolder)
        {
            VerifyRootFolder(rootFolder);

            if (All().Exists(r => r.Path.PathEquals(rootFolder.Path)))
            {
                throw new InvalidOperationException("Root folder already exists.");
            }

            _rootFolderRepository.Insert(rootFolder);

            _commandQueueManager.Push(new RescanFoldersCommand(new List<string> { rootFolder.Path }, FilterFilesType.None, true, null));

            GetDetails(rootFolder);

            return rootFolder;
        }

        public RootFolder Update(RootFolder rootFolder)
        {
            VerifyRootFolder(rootFolder);

            _rootFolderRepository.Update(rootFolder);

            GetDetails(rootFolder);

            return rootFolder;
        }

        public void Remove(int id)
        {
            _rootFolderRepository.Delete(id);
        }

        public RootFolder Get(int id)
        {
            var rootFolder = _rootFolderRepository.Get(id);
            GetDetails(rootFolder);

            return rootFolder;
        }

        public List<RootFolder> AllForTag(int tagId)
        {
            return All().Where(r => r.DefaultTags.Contains(tagId)).ToList();
        }

        public RootFolder GetBestRootFolder(string path)
        {
            var folders = All();
            return GetBestRootFolder(path, folders);
        }

        public RootFolder GetBestRootFolder(string path, List<RootFolder> allRootFolders)
        {
            return allRootFolders.Where(r => PathEqualityComparer.Instance.Equals(r.Path, path) || r.Path.IsParentPath(path))
                .MaxBy(r => r.Path.Length);
        }

        public string GetBestRootFolderPath(string path)
        {
            var folders = All();
            return GetBestRootFolderPath(path, folders);
        }

        public string GetBestRootFolderPath(string path, List<RootFolder> allRootFolders)
        {
            var possibleRootFolder = GetBestRootFolder(path, allRootFolders);

            if (possibleRootFolder == null)
            {
                var osPath = new OsPath(path);

                return osPath.Directory.ToString();
            }

            return possibleRootFolder?.Path;
        }

        private void GetDetails(RootFolder rootFolder)
        {
            Task.Run(() =>
            {
                if (_diskProvider.FolderExists(rootFolder.Path))
                {
                    rootFolder.Accessible = true;
                    rootFolder.FreeSpace = _diskProvider.GetAvailableSpace(rootFolder.Path);
                    rootFolder.TotalSpace = _diskProvider.GetTotalSize(rootFolder.Path);
                }
            }).Wait(5000);
        }

        public void Handle(ModelEvent<RemotePathMapping> message)
        {
            var commands = All()
                .Where(x => x.IsCalibreLibrary &&
                       x.CalibreSettings.Host == message.Model.Host &&
                       x.Path.StartsWith(message.Model.LocalPath))
                .Select(x => new RescanFoldersCommand(new List<string> { x.Path }, FilterFilesType.None, true, null))
                .ToList();

            if (commands.Any())
            {
                _commandQueueManager.PushMany(commands);
            }
        }
    }
}
