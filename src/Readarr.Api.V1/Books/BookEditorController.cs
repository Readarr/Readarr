using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Books;
using NzbDrone.Core.Messaging.Commands;
using Readarr.Http;

namespace Readarr.Api.V1.Books
{
    [V1ApiController("book/editor")]
    public class BookEditorController : Controller
    {
        private readonly IBookService _bookService;
        private readonly IManageCommandQueue _commandQueueManager;

        public BookEditorController(IBookService bookService, IManageCommandQueue commandQueueManager)
        {
            _bookService = bookService;
            _commandQueueManager = commandQueueManager;
        }

        [HttpPut]
        public IActionResult SaveAll([FromBody] BookEditorResource resource)
        {
            var booksToUpdate = _bookService.GetBooks(resource.BookIds);

            foreach (var book in booksToUpdate)
            {
                if (resource.Monitored.HasValue)
                {
                    book.Monitored = resource.Monitored.Value;
                }
            }

            _bookService.UpdateMany(booksToUpdate);
            return Accepted(booksToUpdate.ToResource());
        }

        [HttpDelete]
        public void DeleteBook([FromBody] BookEditorResource resource)
        {
            foreach (var bookId in resource.BookIds)
            {
                _bookService.DeleteBook(bookId, resource.DeleteFiles ?? false, resource.AddImportListExclusion ?? false);
            }
        }
    }
}
