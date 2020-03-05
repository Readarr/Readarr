using System.Collections.Generic;
using System.Linq;
using Readarr.Http.REST;

namespace Readarr.Api.V1.Tracks
{
    public class RenameTrackResource : RestResource
    {
        public int ArtistId { get; set; }
        public int AlbumId { get; set; }
        public int TrackFileId { get; set; }
        public string ExistingPath { get; set; }
        public string NewPath { get; set; }
    }

    public static class RenameTrackResourceMapper
    {
        public static RenameTrackResource ToResource(this NzbDrone.Core.MediaFiles.RenameTrackFilePreview model)
        {
            if (model == null)
            {
                return null;
            }

            return new RenameTrackResource
            {
                ArtistId = model.ArtistId,
                AlbumId = model.AlbumId,
                TrackFileId = model.TrackFileId,
                ExistingPath = model.ExistingPath,
                NewPath = model.NewPath
            };
        }

        public static List<RenameTrackResource> ToResource(this IEnumerable<NzbDrone.Core.MediaFiles.RenameTrackFilePreview> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
