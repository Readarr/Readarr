using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
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
                itemsPerPage = 100000;
            }

            var pagingSpec = new PagingSpec<Book>
            {
                Page = (int)page,
                PageSize = (int)itemsPerPage,
                SortKey = "Id",
                SortDirection = SortDirection.Default
            };
            pagingSpec = _bookService.BooksWithFiles(pagingSpec);

            var publications = OPDSResourceMapper.ToOPDSPublicationsResource((int)page, (int)itemsPerPage, pagingSpec.TotalRecords);
            publications.Publications = MapToResource(pagingSpec.Records, true);

            return publications;
        }

        // /opds/wanted
        [HttpGet("wanted")]
        public OPDSPublicationsResource GetOPDSWantedPublications([FromQuery] int? page,
            [FromQuery] int? itemsPerPage)
        {
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
                itemsPerPage = 100000;
            }

            var pagingSpec = new PagingSpec<Book>
            {
                Page = (int)page,
                PageSize = (int)itemsPerPage,
                SortKey = "Id",
                SortDirection = SortDirection.Default
            };
            pagingSpec.FilterExpressions.Add(v => v.Monitored == true);
            pagingSpec = _bookService.BooksWithoutFiles(pagingSpec);

            var publications = OPDSResourceMapper.ToOPDSPublicationsResource((int)page, (int)itemsPerPage, pagingSpec.TotalRecords);
            publications.Publications = MapToResource(pagingSpec.Records, true);
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
            var publications = new List<OPDSPublicationResource>();
            var metadataTask = Task.Run(() => _authorService.GetAllAuthors());
            for (var i = 0; i < books.Count; i++)
            {
                var images = new List<MediaCover>();
                var book = books[i];
                var bookfiles = _mediaFileService.GetFilesByBook(book.Id);
                var selectedEdition = book.Editions?.Value.Where(x => x.Monitored).SingleOrDefault();
                var ebookEdition = book.Editions?.Value.Where(x => x.IsEbook).FirstOrDefault();
                var covers = selectedEdition?.Images ?? new List<MediaCover>();
                _coverMapper.ConvertToLocalUrls(book.Id, MediaCoverEntity.Book, covers);
                var publication = OPDSResourceMapper.ToOPDSPublicationResource(book, bookfiles, ebookEdition, covers);
                publications.Add(publication);
            }

            return publications;
        }
    }
}
