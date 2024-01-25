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
        public OPDSCatalogResource GetOPDSCatalog()
        {
            /*var metadataTask = Task.Run(() => _authorService.GetAllAuthors());
            var books = _bookService.GetAllBooks();
            var authors = metadataTask.GetAwaiter().GetResult().ToDictionary(x => x.AuthorMetadataId);

            foreach (var book in books)
            {
                book.Author = authors[book.AuthorMetadataId];
            }*/

            var catalog = OPDSResourceMapper.ToOPDSCatalogResource();

            //catalog.Publications = MapToResource(books, wanted);
            return catalog;
        }

        // /opds/publications
        [HttpGet("publications")]
        public OPDSPublicationsResource GetOPDSPublications([FromQuery] int? page,
            [FromQuery] int? itemsPerPage)
        {
            var metadataTask = Task.Run(() => _authorService.GetAllAuthors());
            var books = _bookService.GetAllBooks();
            var authors = metadataTask.GetAwaiter().GetResult().ToDictionary(x => x.AuthorMetadataId);

            if (itemsPerPage == null)
            {
                itemsPerPage = 10;
            }
            else if (itemsPerPage < 10)
            {
                itemsPerPage = 10;
            }

            if (page == null)
            {
                page = 1;
                itemsPerPage = books.Count;
            }

            var pageCount = ((books.Count - 1) / itemsPerPage) + 1;

            if (page > pageCount)
            {
                throw new BadRequestException(string.Format("requested page is greater than the total page count: {0}", page));
            }

            foreach (var book in books)
            {
                book.Author = authors[book.AuthorMetadataId];
            }

            var pubList = MapToResource(books, false);
            var publications = OPDSResourceMapper.ToOPDSPublicationsResource((int)page, (int)itemsPerPage, pubList.Count);

            if (itemsPerPage == pubList.Count)
            {
                publications.Publications = pubList;
            }
            else
            {
                if (page == pageCount)
                {
                    publications.Publications = pubList.GetRange(((int)page - 1) * (int)itemsPerPage, pubList.Count % (int)itemsPerPage);
                }
                else
                {
                    publications.Publications = pubList.GetRange(((int)page - 1) * (int)itemsPerPage, (int)itemsPerPage);
                }
            }

            return publications;
        }

        // /opds/wanted
        [HttpGet("wanted")]
        public OPDSPublicationsResource GetOPDSWantedPublications([FromQuery] int? page,
            [FromQuery] int? itemsPerPage)
        {
            var metadataTask = Task.Run(() => _authorService.GetAllAuthors());
            var books = _bookService.GetAllBooks();
            var authors = metadataTask.GetAwaiter().GetResult().ToDictionary(x => x.AuthorMetadataId);

            if (itemsPerPage == null)
            {
                itemsPerPage = 10;
            }
            else if (itemsPerPage < 10)
            {
                itemsPerPage = 10;
            }

            if (page == null)
            {
                page = 1;
                itemsPerPage = books.Count;
            }

            var pageCount = ((books.Count - 1) / itemsPerPage) + 1;

            if (page > pageCount)
            {
                throw new BadRequestException(string.Format("requested page is greater than the total page count: {0}", page));
            }

            foreach (var book in books)
            {
                book.Author = authors[book.AuthorMetadataId];
            }

            var pubList = MapToResource(books, true);
            var publications = OPDSResourceMapper.ToOPDSPublicationsResource((int)page, (int)itemsPerPage, pubList.Count);

            if (itemsPerPage == pubList.Count)
            {
                publications.Publications = pubList;
            }
            else
            {
                if (page == pageCount)
                {
                    publications.Publications = pubList.GetRange(((int)page - 1) * (int)itemsPerPage, pubList.Count % (int)itemsPerPage);
                }
                else
                {
                    publications.Publications = pubList.GetRange(((int)page - 1) * (int)itemsPerPage, (int)itemsPerPage);
                }
            }

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

            var ebookEdition = book.Editions?.Value.Where(x => x.IsEbook).SingleOrDefault();
            var selectedEdition = book.Editions?.Value.Where(x => x.Monitored).SingleOrDefault();
            var covers = selectedEdition?.Images ?? new List<MediaCover>();
            _coverMapper.ConvertToLocalUrls(book.Id, MediaCoverEntity.Book, covers);
            _coverMapper.ConvertToLocalUrls(book.Id, MediaCoverEntity.Book, images);
            book.Author = author;

            return OPDSResourceMapper.ToOPDSPublicationResource(book, bookfiles, ebookEdition, images);
        }

        protected List<OPDSPublicationResource> MapToResource(List<Book> books, bool wanted)
        {
            var pubclications = new List<OPDSPublicationResource>();
            for (var i = 0; i < books.Count; i++)
            {
                var images = new List<MediaCover>();
                var book = books[i];
                var bookfiles = _mediaFileService.GetFilesByBook(book.Id);
                var selectedEdition = book.Editions?.Value.Where(x => x.Monitored).SingleOrDefault();
                var ebookEdition = book.Editions?.Value.Where(x => x.IsEbook).FirstOrDefault();
                var covers = selectedEdition?.Images ?? new List<MediaCover>();
                _coverMapper.ConvertToLocalUrls(book.Id, MediaCoverEntity.Book, covers);

                //only add publications for which we have a valid bookfile
                if (!bookfiles.Any() && wanted && book.Monitored)
                {
                    var publication = OPDSResourceMapper.ToOPDSPublicationResource(book, bookfiles, ebookEdition, covers);
                    pubclications.Add(publication);
                }
                else if (bookfiles.Any() && !wanted)
                {
                    var publication = OPDSResourceMapper.ToOPDSPublicationResource(book, bookfiles, ebookEdition, covers);
                    pubclications.Add(publication);
                }
            }

            return pubclications;
        }
    }
}
