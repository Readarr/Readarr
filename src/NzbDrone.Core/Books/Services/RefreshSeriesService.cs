using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Music
{
    public interface IRefreshSeriesService
    {
        bool RefreshSeriesInfo(int authorMetadataId, List<Series> remoteAlbums, bool forceAlbumRefresh, bool forceUpdateFileTags, DateTime? lastUpdate);
    }

    public class RefreshSeriesService : RefreshEntityServiceBase<Series, SeriesBookLink>, IRefreshSeriesService
    {
        private readonly ISeriesService _seriesService;
        private readonly ISeriesBookLinkService _linkService;
        private readonly IRefreshSeriesBookLinkService _refreshLinkService;

        public RefreshSeriesService(ISeriesService seriesService,
                                    ISeriesBookLinkService linkService,
                                    IRefreshSeriesBookLinkService refreshLinkService,
                                    IArtistMetadataService artistMetadataService,
                                    Logger logger)
        : base(logger, artistMetadataService)
        {
            _seriesService = seriesService;
            _linkService = linkService;
            _refreshLinkService = refreshLinkService;
        }

        protected override RemoteData GetRemoteData(Series local, List<Series> remote)
        {
            return new RemoteData
            {
                Entity = remote.SingleOrDefault(x => x.ForeignSeriesId == local.ForeignSeriesId)
            };
        }

        protected override bool IsMerge(Series local, Series remote)
        {
            return local.ForeignSeriesId != remote.ForeignSeriesId;
        }

        protected override UpdateResult UpdateEntity(Series local, Series remote)
        {
            if (local.Equals(remote))
            {
                return UpdateResult.None;
            }

            local.UseMetadataFrom(remote);

            return UpdateResult.UpdateTags;
        }

        protected override Series GetEntityByForeignId(Series local)
        {
            return _seriesService.FindById(local.ForeignSeriesId);
        }

        protected override void SaveEntity(Series local)
        {
            // Use UpdateMany to avoid firing the album edited event
            _seriesService.UpdateMany(new List<Series> { local });
        }

        protected override void DeleteEntity(Series local, bool deleteFiles)
        {
            _seriesService.Delete(local.Id);
        }

        protected override List<SeriesBookLink> GetRemoteChildren(Series remote)
        {
            return remote.LinkItems;
        }

        protected override List<SeriesBookLink> GetLocalChildren(Series entity, List<SeriesBookLink> remoteChildren)
        {
            return _linkService.GetLinksBySeries(entity.Id);
        }

        protected override Tuple<SeriesBookLink, List<SeriesBookLink>> GetMatchingExistingChildren(List<SeriesBookLink> existingChildren, SeriesBookLink remote)
        {
            var existingChild = existingChildren.SingleOrDefault(x => x.ForeignId == remote.ForeignId);
            var mergeChildren = new List<SeriesBookLink>();
            return Tuple.Create(existingChild, mergeChildren);
        }

        protected override void PrepareNewChild(SeriesBookLink child, Series entity)
        {
            child.Series = entity;
            child.SeriesId = entity.Id;
            child.BookId = child.Book.Value.Id;
        }

        protected override void PrepareExistingChild(SeriesBookLink local, SeriesBookLink remote, Series entity)
        {
            local.Series = entity;
            local.SeriesId = entity.Id;

            remote.Id = local.Id;
            remote.BookId = local.BookId;
            remote.SeriesId = entity.Id;
        }

        protected override void AddChildren(List<SeriesBookLink> children)
        {
            _linkService.InsertMany(children);
        }

        protected override bool RefreshChildren(SortedChildren localChildren, List<SeriesBookLink> remoteChildren, bool forceChildRefresh, bool forceUpdateFileTags, DateTime? lastUpdate)
        {
            return _refreshLinkService.RefreshSeriesBookLinkInfo(localChildren.Added, localChildren.Updated, localChildren.Merged, localChildren.Deleted, localChildren.UpToDate, remoteChildren, forceUpdateFileTags);
        }

        public bool RefreshSeriesInfo(int authorMetadataId, List<Series> remoteSeries, bool forceAlbumRefresh, bool forceUpdateFileTags, DateTime? lastUpdate)
        {
            bool updated = false;

            var existing = _seriesService.GetByAuthorMetadataId(authorMetadataId);
            var toAdd = remoteSeries.ExceptBy(x => x.ForeignSeriesId, existing, x => x.ForeignSeriesId, StringComparer.Ordinal).ToList();
            var all = toAdd.Union(existing).ToList();

            all.ForEach(x => x.AuthorMetadataId = authorMetadataId);

            _seriesService.InsertMany(toAdd);

            foreach (var item in all)
            {
                updated |= RefreshEntityInfo(item, remoteSeries, true, forceUpdateFileTags, null);
            }

            return updated;
        }
    }
}
