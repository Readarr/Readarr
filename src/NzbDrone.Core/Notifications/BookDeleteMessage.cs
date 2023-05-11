using NzbDrone.Core.Books;

namespace NzbDrone.Core.Notifications
{
    public class BookDeleteMessage
    {
        public string Message { get; set; }
        public Book Book { get; set; }
        public bool DeletedFiles { get; set; }
        public string DeletedFilesMessage { get; set; }

        public override string ToString()
        {
            return Message;
        }

        public BookDeleteMessage(Book book, bool deleteFiles)
        {
            Book = book;
            DeletedFiles = deleteFiles;
            DeletedFilesMessage = DeletedFiles ?
                "Book removed and all files were deleted" :
                "Book removed, files were not deleted";
            Message = book.Title + " - " + DeletedFilesMessage;
        }
    }
}
