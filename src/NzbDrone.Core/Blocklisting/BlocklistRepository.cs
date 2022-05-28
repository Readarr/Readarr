using System.Collections.Generic;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Blocklisting
{
    public interface IBlocklistRepository : IBasicRepository<Blocklist>
    {
        List<Blocklist> BlocklistedByTitle(int authorId, string sourceTitle);
        List<Blocklist> BlocklistedByTorrentInfoHash(int authorId, string torrentInfoHash);
        List<Blocklist> BlocklistedByAuthor(int authorId);
    }

    public class BlocklistRepository : BasicRepository<Blocklist>, IBlocklistRepository
    {
        public BlocklistRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<Blocklist> BlocklistedByTitle(int authorId, string sourceTitle)
        {
            return Query(e => e.AuthorId == authorId && e.SourceTitle.Contains(sourceTitle));
        }

        public List<Blocklist> BlocklistedByTorrentInfoHash(int authorId, string torrentInfoHash)
        {
            return Query(e => e.AuthorId == authorId && e.TorrentInfoHash.Contains(torrentInfoHash));
        }

        public List<Blocklist> BlocklistedByAuthor(int authorId)
        {
            return Query(b => b.AuthorId == authorId);
        }

        protected override SqlBuilder PagedBuilder() => new SqlBuilder(_database.DatabaseType)
            .Join<Blocklist, Author>((b, m) => b.AuthorId == m.Id)
            .Join<Author, AuthorMetadata>((l, r) => l.AuthorMetadataId == r.Id);
        protected override IEnumerable<Blocklist> PagedQuery(SqlBuilder builder) => _database.QueryJoined<Blocklist, Author, AuthorMetadata>(builder,
            (bl, author, metadata) =>
                    {
                        author.Metadata = metadata;
                        bl.Author = author;
                        return bl;
                    });
    }
}
