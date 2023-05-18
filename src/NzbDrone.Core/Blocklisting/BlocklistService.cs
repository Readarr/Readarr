using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books.Events;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Blocklisting
{
    public interface IBlocklistService
    {
        bool Blocklisted(int authorId, ReleaseInfo release);
        PagingSpec<Blocklist> Paged(PagingSpec<Blocklist> pagingSpec);
        void Block(RemoteBook remoteEpisode, string message);
        void Delete(int id);
        void Delete(List<int> ids);
    }

    public class BlocklistService : IBlocklistService,

                                    IExecute<ClearBlocklistCommand>,
                                    IHandle<DownloadFailedEvent>,
                                    IHandleAsync<AuthorDeletedEvent>
    {
        private readonly IBlocklistRepository _blocklistRepository;

        public BlocklistService(IBlocklistRepository blocklistRepository)
        {
            _blocklistRepository = blocklistRepository;
        }

        public bool Blocklisted(int authorId, ReleaseInfo release)
        {
            var blocklistedByTitle = _blocklistRepository.BlocklistedByTitle(authorId, release.Title);

            if (release.DownloadProtocol == DownloadProtocol.Torrent)
            {
                var torrentInfo = release as TorrentInfo;

                if (torrentInfo == null)
                {
                    return false;
                }

                if (torrentInfo.InfoHash.IsNullOrWhiteSpace())
                {
                    return blocklistedByTitle.Where(b => b.Protocol == DownloadProtocol.Torrent)
                                             .Any(b => SameTorrent(b, torrentInfo));
                }

                var blocklistedByTorrentInfohash = _blocklistRepository.BlocklistedByTorrentInfoHash(authorId, torrentInfo.InfoHash);

                return blocklistedByTorrentInfohash.Any(b => SameTorrent(b, torrentInfo));
            }

            return blocklistedByTitle.Where(b => b.Protocol == DownloadProtocol.Usenet)
                                     .Any(b => SameNzb(b, release));
        }

        public PagingSpec<Blocklist> Paged(PagingSpec<Blocklist> pagingSpec)
        {
            return _blocklistRepository.GetPaged(pagingSpec);
        }

        public void Block(RemoteBook remoteEpisode, string message)
        {
            var blocklist = new Blocklist
                            {
                                AuthorId = remoteEpisode.Author.Id,
                                BookIds = remoteEpisode.Books.Select(e => e.Id).ToList(),
                                SourceTitle =  remoteEpisode.Release.Title,
                                Quality = remoteEpisode.ParsedBookInfo.Quality,
                                Date = DateTime.UtcNow,
                                PublishedDate = remoteEpisode.Release.PublishDate,
                                Size = remoteEpisode.Release.Size,
                                Indexer = remoteEpisode.Release.Indexer,
                                Protocol = remoteEpisode.Release.DownloadProtocol,
                                Message = message
                            };

            if (remoteEpisode.Release is TorrentInfo torrentRelease)
            {
                blocklist.TorrentInfoHash = torrentRelease.InfoHash;
            }

            _blocklistRepository.Insert(blocklist);
        }

        public void Delete(int id)
        {
            _blocklistRepository.Delete(id);
        }

        public void Delete(List<int> ids)
        {
            _blocklistRepository.DeleteMany(ids);
        }

        private bool SameNzb(Blocklist item, ReleaseInfo release)
        {
            if (item.PublishedDate == release.PublishDate)
            {
                return true;
            }

            if (!HasSameIndexer(item, release.Indexer) &&
                HasSamePublishedDate(item, release.PublishDate) &&
                HasSameSize(item, release.Size))
            {
                return true;
            }

            return false;
        }

        private bool SameTorrent(Blocklist item, TorrentInfo release)
        {
            if (release.InfoHash.IsNotNullOrWhiteSpace())
            {
                return release.InfoHash.Equals(item.TorrentInfoHash);
            }

            return HasSameIndexer(item, release.Indexer);
        }

        private bool HasSameIndexer(Blocklist item, string indexer)
        {
            if (item.Indexer.IsNullOrWhiteSpace())
            {
                return true;
            }

            return item.Indexer.Equals(indexer, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool HasSamePublishedDate(Blocklist item, DateTime publishedDate)
        {
            if (!item.PublishedDate.HasValue)
            {
                return true;
            }

            return item.PublishedDate.Value.AddMinutes(-2) <= publishedDate &&
                   item.PublishedDate.Value.AddMinutes(2) >= publishedDate;
        }

        private bool HasSameSize(Blocklist item, long size)
        {
            if (!item.Size.HasValue)
            {
                return true;
            }

            var difference = Math.Abs(item.Size.Value - size);

            return difference <= 2.Megabytes();
        }

        public void Execute(ClearBlocklistCommand message)
        {
            _blocklistRepository.Purge();
        }

        public void Handle(DownloadFailedEvent message)
        {
            var blocklist = new Blocklist
            {
                AuthorId = message.AuthorId,
                BookIds = message.BookIds,
                SourceTitle = message.SourceTitle,
                Quality = message.Quality,
                Date = DateTime.UtcNow,
                PublishedDate = DateTime.Parse(message.Data.GetValueOrDefault("publishedDate")),
                Size = long.Parse(message.Data.GetValueOrDefault("size", "0")),
                Indexer = message.Data.GetValueOrDefault("indexer"),
                Protocol = (DownloadProtocol)Convert.ToInt32(message.Data.GetValueOrDefault("protocol")),
                Message = message.Message,
                TorrentInfoHash = message.Data.GetValueOrDefault("torrentInfoHash")
            };

            _blocklistRepository.Insert(blocklist);
        }

        public void HandleAsync(AuthorDeletedEvent message)
        {
            var blocklisted = _blocklistRepository.BlocklistedByAuthor(message.Author.Id);

            _blocklistRepository.DeleteMany(blocklisted);
        }
    }
}
