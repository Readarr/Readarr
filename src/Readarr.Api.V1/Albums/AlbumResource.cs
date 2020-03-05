using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Music;
using Readarr.Api.V1.Artist;
using Readarr.Http.REST;

namespace Readarr.Api.V1.Albums
{
    public class AlbumResource : RestResource
    {
        public string Title { get; set; }
        public string Disambiguation { get; set; }
        public string Overview { get; set; }
        public int ArtistId { get; set; }
        public string ForeignAlbumId { get; set; }
        public bool Monitored { get; set; }
        public Ratings Ratings { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public List<string> Genres { get; set; }
        public ArtistResource Artist { get; set; }
        public List<MediaCover> Images { get; set; }
        public List<Links> Links { get; set; }
        public AlbumStatisticsResource Statistics { get; set; }
        public AddAlbumOptions AddOptions { get; set; }
        public string RemoteCover { get; set; }

        //Hiding this so people don't think its usable (only used to set the initial state)
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Grabbed { get; set; }
    }

    public static class AlbumResourceMapper
    {
        public static AlbumResource ToResource(this Book model)
        {
            if (model == null)
            {
                return null;
            }

            return new AlbumResource
            {
                Id = model.Id,
                ArtistId = model.AuthorId,
                ForeignAlbumId = model.ForeignBookId,
                Monitored = model.Monitored,
                ReleaseDate = model.ReleaseDate,
                Genres = model.Genres,
                Title = model.Title,
                Disambiguation = model.Disambiguation,
                Overview = model.Overview,
                Images = model.Images,
                Links = model.Links,
                Ratings = model.Ratings,
                Artist = model.Author?.Value.ToResource()
            };
        }

        public static Book ToModel(this AlbumResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            var artist = resource.Artist?.ToModel() ?? new NzbDrone.Core.Music.Author();

            return new Book
            {
                Id = resource.Id,
                ForeignBookId = resource.ForeignAlbumId,
                Title = resource.Title,
                Disambiguation = resource.Disambiguation,
                Overview = resource.Overview,
                Images = resource.Images,
                Monitored = resource.Monitored,
                AddOptions = resource.AddOptions,
                Author = artist,
                AuthorMetadata = artist.Metadata.Value
            };
        }

        public static Book ToModel(this AlbumResource resource, Book album)
        {
            var updatedAlbum = resource.ToModel();

            album.ApplyChanges(updatedAlbum);
            album.AlbumReleases = updatedAlbum.AlbumReleases;

            return album;
        }

        public static List<AlbumResource> ToResource(this IEnumerable<Book> models)
        {
            return models?.Select(ToResource).ToList();
        }

        public static List<Book> ToModel(this IEnumerable<AlbumResource> resources)
        {
            return resources.Select(ToModel).ToList();
        }
    }
}
