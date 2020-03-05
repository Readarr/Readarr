using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.MetadataSource;

namespace NzbDrone.Core.Music
{
    public interface IAddAlbumService
    {
        Book AddAlbum(Book album);
        List<Book> AddAlbums(List<Book> albums);
    }

    public class AddAlbumService : IAddAlbumService
    {
        private readonly IArtistService _artistService;
        private readonly IAddArtistService _addArtistService;
        private readonly IAlbumService _albumService;
        private readonly IProvideBookInfo _albumInfo;
        private readonly IImportListExclusionService _importListExclusionService;
        private readonly Logger _logger;

        public AddAlbumService(IArtistService artistService,
                               IAddArtistService addArtistService,
                               IAlbumService albumService,
                               IProvideBookInfo albumInfo,
                               IImportListExclusionService importListExclusionService,
                               Logger logger)
        {
            _artistService = artistService;
            _addArtistService = addArtistService;
            _albumService = albumService;
            _albumInfo = albumInfo;
            _importListExclusionService = importListExclusionService;
            _logger = logger;
        }

        public Book AddAlbum(Book album)
        {
            _logger.Debug($"Adding album {album}");

            album = AddSkyhookData(album);

            // Remove any import list exclusions preventing addition
            _importListExclusionService.Delete(album.ForeignBookId);
            _importListExclusionService.Delete(album.AuthorMetadata.Value.ForeignAuthorId);

            // Note it's a manual addition so it's not deleted on next refresh
            album.AddOptions.AddType = AlbumAddType.Manual;

            // Add the artist if necessary
            var dbArtist = _artistService.FindById(album.AuthorMetadata.Value.ForeignAuthorId);
            if (dbArtist == null)
            {
                var artist = album.Author.Value;

                artist.Metadata.Value.ForeignAuthorId = album.AuthorMetadata.Value.ForeignAuthorId;

                dbArtist = _addArtistService.AddArtist(artist, false);
            }

            album.AuthorMetadataId = dbArtist.AuthorMetadataId;
            _albumService.AddAlbum(album);

            return album;
        }

        public List<Book> AddAlbums(List<Book> albums)
        {
            var added = DateTime.UtcNow;
            var addedAlbums = new List<Book>();

            foreach (var a in albums)
            {
                a.Added = added;
                addedAlbums.Add(AddAlbum(a));
            }

            return addedAlbums;
        }

        private Book AddSkyhookData(Book newAlbum)
        {
            Tuple<string, Book, List<AuthorMetadata>> tuple = null;
            try
            {
                tuple = _albumInfo.GetBookInfo(newAlbum.ForeignBookId);
            }
            catch (AlbumNotFoundException)
            {
                _logger.Error("Album with MusicBrainz Id {0} was not found, it may have been removed from Musicbrainz.", newAlbum.ForeignBookId);

                throw new ValidationException(new List<ValidationFailure>
                                              {
                                                  new ValidationFailure("MusicbrainzId", "An album with this ID was not found", newAlbum.ForeignBookId)
                                              });
            }

            newAlbum.UseMetadataFrom(tuple.Item2);
            newAlbum.Added = DateTime.UtcNow;

            var metadata = tuple.Item3.Single(x => x.ForeignAuthorId == tuple.Item1);
            newAlbum.AuthorMetadata = metadata;

            return newAlbum;
        }
    }
}
