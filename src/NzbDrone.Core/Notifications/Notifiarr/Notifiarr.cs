using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.HealthCheck;

namespace NzbDrone.Core.Notifications.Notifiarr
{
    public class Notifiarr : NotificationBase<NotifiarrSettings>
    {
        private readonly INotifiarrProxy _proxy;

        public Notifiarr(INotifiarrProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Link => "https://notifiarr.com";
        public override string Name => "Notifiarr";

        public override void OnGrab(GrabMessage message)
        {
            var author = message.Author;
            var remoteBook = message.Book;
            var releaseGroup = remoteBook.ParsedBookInfo.ReleaseGroup;
            var variables = new StringDictionary();

            variables.Add("Readarr_EventType", "Grab");
            variables.Add("Readarr_Author_Id", author.Id.ToString());
            variables.Add("Readarr_Author_Name", author.Metadata.Value.Name);
            variables.Add("Readarr_Author_GRId", author.Metadata.Value.ForeignAuthorId);
            variables.Add("Readarr_Release_BookCount", remoteBook.Books.Count.ToString());
            variables.Add("Readarr_Release_BookReleaseDates", string.Join(",", remoteBook.Books.Select(e => e.ReleaseDate)));
            variables.Add("Readarr_Release_BookTitles", string.Join("|", remoteBook.Books.Select(e => e.Title)));
            variables.Add("Readarr_Release_BookIds", string.Join("|", remoteBook.Books.Select(e => e.Id.ToString())));
            variables.Add("Readarr_Release_GRIds", remoteBook.Books.Select(x => x.Editions.Value.Single(e => e.Monitored).ForeignEditionId).ConcatToString("|"));
            variables.Add("Readarr_Release_Title", remoteBook.Release.Title);
            variables.Add("Readarr_Release_Indexer", remoteBook.Release.Indexer ?? string.Empty);
            variables.Add("Readarr_Release_Size", remoteBook.Release.Size.ToString());
            variables.Add("Readarr_Release_Quality", remoteBook.ParsedBookInfo.Quality.Quality.Name);
            variables.Add("Readarr_Release_QualityVersion", remoteBook.ParsedBookInfo.Quality.Revision.Version.ToString());
            variables.Add("Readarr_Release_ReleaseGroup", releaseGroup ?? string.Empty);
            variables.Add("Readarr_Download_Client", message.DownloadClient ?? string.Empty);
            variables.Add("Readarr_Download_Id", message.DownloadId ?? string.Empty);

            _proxy.SendNotification(variables, Settings);
        }

        public override void OnReleaseImport(BookDownloadMessage message)
        {
            var author = message.Author;
            var book = message.Book;
            var variables = new StringDictionary();

            variables.Add("Readarr_EventType", "Download");
            variables.Add("Readarr_Author_Id", author.Id.ToString());
            variables.Add("Readarr_Author_Name", author.Metadata.Value.Name);
            variables.Add("Readarr_Author_Path", author.Path);
            variables.Add("Readarr_Author_GRId", author.Metadata.Value.ForeignAuthorId);
            variables.Add("Readarr_Book_Id", book.Id.ToString());
            variables.Add("Readarr_Book_Title", book.Title);
            variables.Add("Readarr_Book_GRId", book.Editions.Value.Single(e => e.Monitored).ForeignEditionId.ToString());
            variables.Add("Readarr_Book_ReleaseDate", book.ReleaseDate.ToString());
            variables.Add("Readarr_Download_Client", message.DownloadClient ?? string.Empty);
            variables.Add("Readarr_Download_Id", message.DownloadId ?? string.Empty);

            if (message.BookFiles.Any())
            {
                variables.Add("Readarr_AddedBookPaths", string.Join("|", message.BookFiles.Select(e => e.Path)));
            }

            if (message.OldFiles.Any())
            {
                variables.Add("Readarr_DeletedPaths", string.Join("|", message.OldFiles.Select(e => e.Path)));
            }

            _proxy.SendNotification(variables, Settings);
        }

        public override void OnAuthorDelete(AuthorDeleteMessage deleteMessage)
        {
            var author = deleteMessage.Author;
            var variables = new StringDictionary();

            variables.Add("Readarr_EventType", "AuthorDelete");
            variables.Add("Readarr_Author_Id", author.Id.ToString());
            variables.Add("Readarr_Author_Name", author.Name);
            variables.Add("Readarr_Author_Path", author.Path);
            variables.Add("Readarr_Author_GoodreadsId", author.ForeignAuthorId);
            variables.Add("Readarr_Author_DeletedFiles", deleteMessage.DeletedFiles.ToString());

            _proxy.SendNotification(variables, Settings);
        }

        public override void OnBookDelete(BookDeleteMessage deleteMessage)
        {
            var author = deleteMessage.Book.Author.Value;
            var book = deleteMessage.Book;

            var variables = new StringDictionary();

            variables.Add("Readarr_EventType", "BookDelete");
            variables.Add("Readarr_Author_Id", author.Id.ToString());
            variables.Add("Readarr_Author_Name", author.Name);
            variables.Add("Readarr_Author_Path", author.Path);
            variables.Add("Readarr_Author_GoodreadsId", author.ForeignAuthorId);
            variables.Add("Readarr_Book_Id", book.Id.ToString());
            variables.Add("Readarr_Book_Title", book.Title);
            variables.Add("Readarr_Book_GoodreadsId", book.ForeignBookId);
            variables.Add("Readarr_Book_DeletedFiles", deleteMessage.DeletedFiles.ToString());

            _proxy.SendNotification(variables, Settings);
        }

        public override void OnBookFileDelete(BookFileDeleteMessage deleteMessage)
        {
            var author = deleteMessage.Book.Author.Value;
            var book = deleteMessage.Book;
            var bookFile = deleteMessage.BookFile;
            var edition = bookFile.Edition.Value;

            var variables = new StringDictionary();

            variables.Add("Readarr_EventType", "BookFileDelete");
            variables.Add("Readarr_Delete_Reason", deleteMessage.Reason.ToString());
            variables.Add("Readarr_Author_Id", author.Id.ToString());
            variables.Add("Readarr_Author_Name", author.Name);
            variables.Add("Readarr_Author_GoodreadsId", author.ForeignAuthorId);
            variables.Add("Readarr_Book_Id", book.Id.ToString());
            variables.Add("Readarr_Book_Title", book.Title);
            variables.Add("Readarr_Book_GoodreadsId", book.ForeignBookId);
            variables.Add("Readarr_BookFile_Id", bookFile.Id.ToString());
            variables.Add("Readarr_BookFile_Path", bookFile.Path);
            variables.Add("Readarr_BookFile_Quality", bookFile.Quality.Quality.Name);
            variables.Add("Readarr_BookFile_QualityVersion", bookFile.Quality.Revision.Version.ToString());
            variables.Add("Readarr_BookFile_ReleaseGroup", bookFile.ReleaseGroup ?? string.Empty);
            variables.Add("Readarr_BookFile_SceneName", bookFile.SceneName ?? string.Empty);
            variables.Add("Readarr_BookFile_Edition_Id", edition.Id.ToString());
            variables.Add("Readarr_BookFile_Edition_Name", edition.Title);
            variables.Add("Readarr_BookFile_Edition_GoodreadsId", edition.ForeignEditionId);
            variables.Add("Readarr_BookFile_Edition_Isbn13", edition.Isbn13);
            variables.Add("Readarr_BookFile_Edition_Asin", edition.Asin);

            _proxy.SendNotification(variables, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            var variables = new StringDictionary();

            variables.Add("Readarr_EventType", "HealthIssue");
            variables.Add("Readarr_Health_Issue_Level", Enum.GetName(typeof(HealthCheckResult), healthCheck.Type));
            variables.Add("Readarr_Health_Issue_Message", healthCheck.Message);
            variables.Add("Readarr_Health_Issue_Type", healthCheck.Source.Name);
            variables.Add("Readarr_Health_Issue_Wiki", healthCheck.WikiUrl.ToString() ?? string.Empty);

            _proxy.SendNotification(variables, Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_proxy.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
