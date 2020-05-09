using System.Collections.Generic;
using System.Linq;
using Readarr.Http.REST;

namespace Readarr.Api.V1.Albums
{
    public class TagDifference
    {
        public string Field { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }

    public class RetagTrackResource : RestResource
    {
        public int AuthorId { get; set; }
        public int BookId { get; set; }
        public List<int> TrackNumbers { get; set; }
        public int TrackFileId { get; set; }
        public string Path { get; set; }
        public List<TagDifference> Changes { get; set; }
    }

    public static class RetagTrackResourceMapper
    {
        public static RetagTrackResource ToResource(this NzbDrone.Core.MediaFiles.RetagTrackFilePreview model)
        {
            if (model == null)
            {
                return null;
            }

            return new RetagTrackResource
            {
                AuthorId = model.AuthorId,
                BookId = model.BookId,
                TrackNumbers = model.TrackNumbers.ToList(),
                TrackFileId = model.TrackFileId,
                Path = model.Path,
                Changes = model.Changes.Select(x => new TagDifference
                {
                    Field = x.Key,
                    OldValue = x.Value.Item1,
                    NewValue = x.Value.Item2
                }).ToList()
            };
        }

        public static List<RetagTrackResource> ToResource(this IEnumerable<NzbDrone.Core.MediaFiles.RetagTrackFilePreview> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
