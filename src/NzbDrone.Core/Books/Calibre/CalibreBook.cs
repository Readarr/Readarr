using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.Books.Calibre
{
    public class CalibreBook
    {
        [JsonProperty("format_metadata")]
        public Dictionary<string, CalibreBookFormat> Formats { get; set; }
    }

    public class CalibreBookFormat
    {
        public string Path { get; set; }

        public long Size { get; set; }

        [JsonProperty("mtime")]
        public DateTime LastModified { get; set; }
    }
}
