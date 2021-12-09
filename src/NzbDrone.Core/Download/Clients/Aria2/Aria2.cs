using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CookComputing.XmlRpc;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.Aria2
{
    public class Aria2 : TorrentClientBase<Aria2Settings>
    {
        private readonly IAria2Proxy _proxy;

        public override string Name => "Aria2";

        public Aria2(IAria2Proxy proxy,
                        ITorrentFileInfoReader torrentFileInfoReader,
                        IHttpClient httpClient,
                        IConfigService configService,
                        IDiskProvider diskProvider,
                        IRemotePathMappingService remotePathMappingService,
                        Logger logger)
            : base(torrentFileInfoReader, httpClient, configService, diskProvider, remotePathMappingService, logger)
        {
            _proxy = proxy;
        }

        protected override string AddFromMagnetLink(RemoteBook remoteEpisode, string hash, string magnetLink)
        {
            var gid = _proxy.AddMagnet(Settings, magnetLink);

            var tries = 10;
            var retryDelay = 500;

            // Wait a bit for the magnet to be resolved.
            if (!WaitForTorrent(gid, hash, tries, retryDelay))
            {
                _logger.Warn($"Aria2 could not add magnent within {tries * retryDelay / 1000} seconds, download may remain stuck: {magnetLink}.");
                return hash;
            }

            _logger.Debug($"Äria2 AddFromMagnetLink '{hash}' -> '{gid}'");

            return hash;
        }

        protected override string AddFromTorrentFile(RemoteBook remoteEpisode, string hash, string filename, byte[] fileContent)
        {
            var gid = _proxy.AddTorrent(Settings, fileContent);

            var tries = 10;
            var retryDelay = 500;

            // Wait a bit for the magnet to be resolved.
            if (!WaitForTorrent(gid, hash, tries, retryDelay))
            {
                _logger.Warn($"Aria2 could not add torrent within {tries * retryDelay / 1000} seconds, download may remain stuck: {filename}.");
                return hash;
            }

            return hash;
        }

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            var torrents = _proxy.GetTorrents(Settings);

            foreach (var torrent in torrents)
            {
                var firstFile = torrent.Files?.FirstOrDefault();

                //skip metadata download
                if (firstFile?.Path?.Contains("[METADATA]") == true)
                {
                    continue;
                }

                long completedLength = long.Parse(torrent.CompletedLength);
                long totalLength = long.Parse(torrent.TotalLength);
                long uploadedLength = long.Parse(torrent.UploadLength);
                long downloadSpeed = long.Parse(torrent.DownloadSpeed);

                var sta = DownloadItemStatus.Failed;
                var title = "";

                if (torrent.Bittorrent?.ContainsKey("info") == true && ((XmlRpcStruct)torrent.Bittorrent["info"]).ContainsKey("name"))
                {
                    title = ((XmlRpcStruct)torrent.Bittorrent["info"])["name"].ToString();
                }

                switch (torrent.Status)
                {
                    case "active":
                        sta = DownloadItemStatus.Downloading;
                        break;
                    case "waiting":
                        sta = DownloadItemStatus.Queued;
                        break;
                    case "paused":
                        sta = DownloadItemStatus.Paused;
                        break;
                    case "error":
                        sta = DownloadItemStatus.Failed;
                        break;
                    case "complete":
                        sta = DownloadItemStatus.Completed;
                        break;
                    case "removed":
                        sta = DownloadItemStatus.Failed;
                        break;
                }

                _logger.Debug($"- aria2 getstatus hash:'{torrent.InfoHash}' gid:'{torrent.Gid}' sta:'{sta}' tot:{totalLength} comp:'{completedLength}'");

                yield return new DownloadClientItem()
                {
                    CanMoveFiles = false,
                    CanBeRemoved = true,
                    Category = null,
                    DownloadClientInfo = DownloadClientItemClientInfo.FromDownloadClient(this),
                    DownloadId = torrent.InfoHash?.ToUpper(),
                    IsEncrypted = false,
                    Message = torrent.ErrorMessage,
                    OutputPath = _remotePathMappingService.RemapRemoteToLocal(Settings.Host, new OsPath(torrent.Dir)),
                    RemainingSize = totalLength - completedLength,
                    RemainingTime = downloadSpeed == 0 ? (TimeSpan?)null : new TimeSpan(0, 0, (int)((totalLength - completedLength) / downloadSpeed)),
                    Removed = torrent.Status == "removed",
                    SeedRatio = totalLength > 0 ? (double)uploadedLength / totalLength : 0,
                    Status = sta,
                    Title = title,
                    TotalSize = totalLength,
                };
            }
        }

        public override void RemoveItem(DownloadClientItem item, bool deleteData)
        {
            //Aria2 doesn't support file deletion: https://github.com/aria2/aria2/issues/728
            var hash = item.DownloadId.ToLower();
            var aria2Item = _proxy.GetTorrents(Settings).FirstOrDefault(t => t.InfoHash?.ToLower() == hash);

            if (aria2Item == null)
            {
                _logger.Error($"Aria2 could not find infoHash '{hash}' for deletion.");
                return;
            }

            _logger.Debug($"Aria2 removing hash:'{hash}' gid:'{aria2Item.Gid}'");

            if (!_proxy.RemoveTorrent(Settings, aria2Item.Gid))
            {
                _logger.Error($"Aria2 error while deleting {hash}.");
                return;
            }

            if (deleteData)
            {
                DeleteItemData(item);
            }
        }

        public override DownloadClientInfo GetStatus()
        {
            var destDir = _proxy.GetGlobals(Settings);

            return new DownloadClientInfo
            {
                IsLocalhost = Settings.Host.Contains("127.0.0.1") || Settings.Host.Contains("localhost"),
                OutputRootFolders = new List<OsPath> { _remotePathMappingService.RemapRemoteToLocal(Settings.Host, new OsPath(destDir["dir"])) }
            };
        }

        private bool WaitForTorrent(string gid, string hash, int tries, int retryDelay)
        {
            for (var i = 0; i < tries; i++)
            {
                var found = _proxy.GetFromGID(Settings, gid);

                if (found?.InfoHash?.ToLower() == hash?.ToLower())
                {
                    return true;
                }

                Thread.Sleep(retryDelay);
            }

            _logger.Debug("Could not find hash {0} in {1} tries at {2} ms intervals.", hash, tries, retryDelay);

            return false;
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());

            if (failures.HasErrors())
            {
                return;
            }
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                var version = _proxy.GetVersion(Settings);

                if (new Version(version) < new Version("1.34.0"))
                {
                    return new ValidationFailure(string.Empty, "Aria2 version should be at least 1.34.0. Version reported is {0}", version);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to test Aria2");

                return new NzbDroneValidationFailure("Host", "Unable to connect to Aria2")
                {
                    DetailedDescription = ex.Message
                };
            }

            return null;
        }
    }
}
