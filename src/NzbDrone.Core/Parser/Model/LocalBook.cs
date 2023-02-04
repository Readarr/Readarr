using System;
using System.Collections.Generic;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles.BookImport.Identification;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser.Model
{
    public class LocalBook
    {
        public string Path { get; set; }
        public int CalibreId { get; set; }
        public int Part { get; set; }
        public int PartCount { get; set; }
        public long Size { get; set; }
        public DateTime Modified { get; set; }
        public ParsedTrackInfo FileTrackInfo { get; set; }
        public ParsedBookInfo FolderTrackInfo { get; set; }
        public ParsedBookInfo DownloadClientBookInfo { get; set; }
        public List<string> AcoustIdResults { get; set; }
        public Author Author { get; set; }
        public Book Book { get; set; }
        public Edition Edition { get; set; }
        public Distance Distance { get; set; }
        public QualityModel Quality { get; set; }
        public bool ExistingFile { get; set; }
        public bool AdditionalFile { get; set; }
        public bool SceneSource { get; set; }
        public string ReleaseGroup { get; set; }
        public string SceneName { get; set; }

        public override string ToString()
        {
            return Path;
        }
    }
}
