using System;
using System.Collections.Generic;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles
{
    public static class MediaFileExtensions
    {
        private static Dictionary<string, Quality> _fileExtensions;

        static MediaFileExtensions()
        {
            _fileExtensions = new Dictionary<string, Quality>(StringComparer.OrdinalIgnoreCase)
            {
                { ".epub", Quality.Unknown },
                { ".mobi", Quality.Unknown },
                { ".azw3", Quality.Unknown },
                { ".pdf", Quality.Unknown }
            };
        }

        public static HashSet<string> Extensions => new HashSet<string>(_fileExtensions.Keys, StringComparer.OrdinalIgnoreCase);

        public static Quality GetQualityForExtension(string extension)
        {
            if (_fileExtensions.ContainsKey(extension))
            {
                return _fileExtensions[extension];
            }

            return Quality.Unknown;
        }
    }
}
