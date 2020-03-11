using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Music
{
    public interface ISeriesRepository : IBasicRepository<Series>
    {
        Series FindById(string foreignSeriesId);
        List<Series> GetByAuthorMetadataId(int authorMetadataId);
        List<Series> GetByAuthorId(int authorId);
    }

    public class SeriesRepository : BasicRepository<Series>, ISeriesRepository
    {
        public SeriesRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public Series FindById(string foreignSeriesId)
        {
            return Query(x => x.ForeignSeriesId == foreignSeriesId).SingleOrDefault();
        }

        public List<Series> GetByAuthorMetadataId(int authorMetadataId)
        {
            return Query(x => x.AuthorMetadataId == authorMetadataId);
        }

        public List<Series> GetByAuthorId(int authorId)
        {
            return Query(Builder().Join<Series, Author>((l, r) => l.AuthorMetadataId == r.AuthorMetadataId)
                         .Where<Author>(x => x.Id == authorId));
        }
    }
}
