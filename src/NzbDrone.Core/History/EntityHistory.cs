using System;
using System.Collections.Generic;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.History
{
    public class EntityHistory : ModelBase
    {
        public const string DOWNLOAD_CLIENT = "downloadClient";

        public EntityHistory()
        {
            Data = new Dictionary<string, string>();
        }

        public int BookId { get; set; }
        public int AuthorId { get; set; }
        public string SourceTitle { get; set; }
        public QualityModel Quality { get; set; }
        public DateTime Date { get; set; }
        public Book Book { get; set; }
        public Author Author { get; set; }
        public EntityHistoryEventType EventType { get; set; }
        public Dictionary<string, string> Data { get; set; }

        public string DownloadId { get; set; }
    }

    public enum EntityHistoryEventType
    {
        Unknown = 0,
        Grabbed = 1,
        BookFileImported = 3,
        DownloadFailed = 4,
        BookFileDeleted = 5,
        BookFileRenamed = 6,
        BookImportIncomplete = 7,
        DownloadImported = 8,
        BookFileRetagged = 9,
        DownloadIgnored = 10
    }
}
