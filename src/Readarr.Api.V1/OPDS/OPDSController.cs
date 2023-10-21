using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DryIoc.ImTools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using Readarr.Http;
using Readarr.Http.REST;

namespace Readarr.Api.V1.OPDS
{
    [V1ApiController]
    public class OPDSController : Controller
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly IAuthorService _authorService;
        private readonly IBookService _bookService;
        private readonly IEditionService _editionService;
        private readonly IContentTypeProvider _mimeTypeProvider;
        private readonly IMapCoversToLocal _coverMapper;

        public OPDSController(IAuthorService authorService,
                          IBookService bookService,
                          IEditionService editionService,
                          IMapCoversToLocal coverMapper,
                          IMediaFileService mediaFileService)
        {
            _authorService = authorService;
            _bookService = bookService;
            _editionService = editionService;
            _mimeTypeProvider = new FileExtensionContentTypeProvider();
            _coverMapper = coverMapper;
            _mediaFileService = mediaFileService;
        }

        // /opds
        [HttpGet]
        public OPDSCatalogResource GetOPDSCatalog([FromQuery] bool availableBooks = true)
        {
            var metadataTask = Task.Run(() => _authorService.GetAllAuthors());
            var books = _bookService.GetAllBooks();
            var authors = metadataTask.GetAwaiter().GetResult().ToDictionary(x => x.AuthorMetadataId);

            foreach (var book in books)
            {
                book.Author = authors[book.AuthorMetadataId];
            }

            var catalog = OPDSResourceMapper.ToOPDSCatalogResource();
            catalog.Publications = MapToResource(books, false);
            return catalog;
        }

        // /opds/publications
        [HttpGet("publications")]
        public OPDSPublicationsResource GetOPDSPublications()
        {
            var metadataTask = Task.Run(() => _authorService.GetAllAuthors());
            var books = _bookService.GetAllBooks();
            var authors = metadataTask.GetAwaiter().GetResult().ToDictionary(x => x.AuthorMetadataId);

            foreach (var book in books)
            {
                book.Author = authors[book.AuthorMetadataId];
            }

            var publications = OPDSResourceMapper.ToOPDSPublicationsResource();
            publications.Publications = MapToResource(books, false);
            return publications;
        }

        // /opds/publications/{int:id}
        [HttpGet("publications/{id:int}")]
        public OPDSPublicationResource GetOPDSPublication(int id)
        {
            var images = new List<MediaCover>();
            var metadataTask = Task.Run(() => _authorService.GetAllAuthors());
            var book = _bookService.GetBook(id);
            var author = _authorService.GetAuthor(book.AuthorId);
            var bookfiles = _mediaFileService.GetFilesByBook(book.Id);

            if (!bookfiles.Any())
            {
                throw new BadRequestException("No book files exist for the given book id");
            }

            var selectedEdition = book.Editions?.Value.Where(x => x.Monitored).SingleOrDefault();
            var covers = selectedEdition?.Images ?? new List<MediaCover>();
            _coverMapper.ConvertToLocalUrls(book.Id, MediaCoverEntity.Book, covers);
            _coverMapper.ConvertToLocalUrls(book.Id, MediaCoverEntity.Book, images);
            book.Author = author;

            return OPDSResourceMapper.ToOPDSPublicationResource(book, bookfiles, images);
        }

        protected List<OPDSPublicationResource> MapToResource(List<Book> books, bool availableBooks)
        {
            var pubclications = new List<OPDSPublicationResource>();
            for (var i = 0; i < books.Count; i++)
            {
                var images = new List<MediaCover>();
                var book = books[i];
                var bookfiles = _mediaFileService.GetFilesByBook(book.Id);
                var selectedEdition = book.Editions?.Value.Where(x => x.Monitored).SingleOrDefault();
                var covers = selectedEdition?.Images ?? new List<MediaCover>();
                _coverMapper.ConvertToLocalUrls(book.Id, MediaCoverEntity.Book, covers);

                //only add publications for which we have a valid bookfile
                if (!bookfiles.Any())
                {
                    continue;
                }

                var publication = OPDSResourceMapper.ToOPDSPublicationResource(book, bookfiles, covers);
                pubclications.Add(publication);
            }

            return pubclications;
        }
    }
}
