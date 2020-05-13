using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.SignalR;
using Readarr.Http;
using Readarr.Http.Extensions;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace Readarr.Api.V1.TrackFiles
{
    public class TrackFileModule : ReadarrRestModuleWithSignalR<TrackFileResource, BookFile>,
                                 IHandle<BookFileAddedEvent>,
                                 IHandle<BookFileDeletedEvent>
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly IDeleteMediaFiles _mediaFileDeletionService;
        private readonly IAudioTagService _audioTagService;
        private readonly IAuthorService _authorService;
        private readonly IBookService _bookService;
        private readonly IUpgradableSpecification _upgradableSpecification;

        public TrackFileModule(IBroadcastSignalRMessage signalRBroadcaster,
                               IMediaFileService mediaFileService,
                               IDeleteMediaFiles mediaFileDeletionService,
                               IAudioTagService audioTagService,
                               IAuthorService authorService,
                               IBookService bookService,
                               IUpgradableSpecification upgradableSpecification)
            : base(signalRBroadcaster)
        {
            _mediaFileService = mediaFileService;
            _mediaFileDeletionService = mediaFileDeletionService;
            _audioTagService = audioTagService;
            _authorService = authorService;
            _bookService = bookService;
            _upgradableSpecification = upgradableSpecification;

            GetResourceById = GetTrackFile;
            GetResourceAll = GetTrackFiles;
            UpdateResource = SetQuality;
            DeleteResource = DeleteTrackFile;

            Put("/editor", trackFiles => SetQuality());
            Delete("/bulk", trackFiles => DeleteTrackFiles());
        }

        private TrackFileResource MapToResource(BookFile trackFile)
        {
            if (trackFile.BookId > 0 && trackFile.Author != null && trackFile.Author.Value != null)
            {
                return trackFile.ToResource(trackFile.Author.Value, _upgradableSpecification);
            }
            else
            {
                return trackFile.ToResource();
            }
        }

        private TrackFileResource GetTrackFile(int id)
        {
            var resource = MapToResource(_mediaFileService.Get(id));
            resource.AudioTags = _audioTagService.ReadTags(resource.Path);
            return resource;
        }

        private List<TrackFileResource> GetTrackFiles()
        {
            var authorIdQuery = Request.Query.AuthorId;
            var trackFileIdsQuery = Request.Query.TrackFileIds;
            var bookIdQuery = Request.Query.BookId;
            var unmappedQuery = Request.Query.Unmapped;

            if (!authorIdQuery.HasValue && !trackFileIdsQuery.HasValue && !bookIdQuery.HasValue && !unmappedQuery.HasValue)
            {
                throw new Readarr.Http.REST.BadRequestException("authorId, bookId, trackFileIds or unmapped must be provided");
            }

            if (unmappedQuery.HasValue && Convert.ToBoolean(unmappedQuery.Value))
            {
                var files = _mediaFileService.GetUnmappedFiles();
                return files.ConvertAll(f => MapToResource(f));
            }

            if (authorIdQuery.HasValue && !bookIdQuery.HasValue)
            {
                int authorId = Convert.ToInt32(authorIdQuery.Value);
                var artist = _authorService.GetAuthor(authorId);

                return _mediaFileService.GetFilesByAuthor(authorId).ConvertAll(f => f.ToResource(artist, _upgradableSpecification));
            }

            if (bookIdQuery.HasValue)
            {
                string bookIdValue = bookIdQuery.Value.ToString();

                var bookIds = bookIdValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => Convert.ToInt32(e))
                    .ToList();

                var result = new List<TrackFileResource>();
                foreach (var bookId in bookIds)
                {
                    var album = _bookService.GetBook(bookId);
                    var albumArtist = _authorService.GetAuthor(album.AuthorId);
                    result.AddRange(_mediaFileService.GetFilesByBook(album.Id).ConvertAll(f => f.ToResource(albumArtist, _upgradableSpecification)));
                }

                return result;
            }
            else
            {
                string trackFileIdsValue = trackFileIdsQuery.Value.ToString();

                var trackFileIds = trackFileIdsValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                        .Select(e => Convert.ToInt32(e))
                                                        .ToList();

                // trackfiles will come back with the artist already populated
                var trackFiles = _mediaFileService.Get(trackFileIds);
                return trackFiles.ConvertAll(e => MapToResource(e));
            }
        }

        private void SetQuality(TrackFileResource trackFileResource)
        {
            var trackFile = _mediaFileService.Get(trackFileResource.Id);
            trackFile.Quality = trackFileResource.Quality;
            _mediaFileService.Update(trackFile);
        }

        private object SetQuality()
        {
            var resource = Request.Body.FromJson<TrackFileListResource>();
            var trackFiles = _mediaFileService.Get(resource.TrackFileIds);

            foreach (var trackFile in trackFiles)
            {
                if (resource.Quality != null)
                {
                    trackFile.Quality = resource.Quality;
                }
            }

            _mediaFileService.Update(trackFiles);

            return ResponseWithCode(trackFiles.ConvertAll(f => f.ToResource(trackFiles.First().Author.Value, _upgradableSpecification)),
                               Nancy.HttpStatusCode.Accepted);
        }

        private void DeleteTrackFile(int id)
        {
            var trackFile = _mediaFileService.Get(id);

            if (trackFile == null)
            {
                throw new NzbDroneClientException(HttpStatusCode.NotFound, "Track file not found");
            }

            if (trackFile.BookId > 0 && trackFile.Author != null && trackFile.Author.Value != null)
            {
                _mediaFileDeletionService.DeleteTrackFile(trackFile.Author.Value, trackFile);
            }
            else
            {
                _mediaFileDeletionService.DeleteTrackFile(trackFile, "Unmapped_Files");
            }
        }

        private object DeleteTrackFiles()
        {
            var resource = Request.Body.FromJson<TrackFileListResource>();
            var trackFiles = _mediaFileService.Get(resource.TrackFileIds);
            var artist = trackFiles.First().Author.Value;

            foreach (var trackFile in trackFiles)
            {
                _mediaFileDeletionService.DeleteTrackFile(artist, trackFile);
            }

            return new object();
        }

        public void Handle(BookFileAddedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.BookFile));
        }

        public void Handle(BookFileDeletedEvent message)
        {
            BroadcastResourceChange(ModelAction.Deleted, MapToResource(message.BookFile));
        }
    }
}
