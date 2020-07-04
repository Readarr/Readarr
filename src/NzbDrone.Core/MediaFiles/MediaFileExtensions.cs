using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles
{
    public static class MediaFileExtensions
    {
        private static readonly Dictionary<string, Quality> _textExtensions;
        private static readonly Dictionary<string, Quality> _audioExtensions;

        static MediaFileExtensions()
        {
            _textExtensions = new Dictionary<string, Quality>(StringComparer.OrdinalIgnoreCase)
            {
                { ".epub", Quality.EPUB },
                { ".mobi", Quality.MOBI },
                { ".azw3", Quality.AZW3 },
                { ".pdf", Quality.PDF },
            };

            _audioExtensions = new Dictionary<string, Quality>(StringComparer.OrdinalIgnoreCase)
            {
                { ".mp3",   Quality.MP3_320 },
                { ".flac",  Quality.FLAC },
                { ".alac",  Quality.ALAC },
                { ".mp1",   Quality.MP1 },
                { ".mp2",   Quality.MP2 },

                // { ".mp3",   Quality.MP3VBR },
                // { ".mp3",   Quality.MP3CBR },
                { ".ape",   Quality.APE },
                { ".wma",   Quality.WMA },
                { ".wav",   Quality.WAV },
                { ".wv",    Quality.WAVPACK },
                { ".acc",   Quality.AAC },

                // { ".flac",  Quality.AACVBR },
                { ".ogg",   Quality.OGG },
                { ".opus",  Quality.OPUS },
            };
        }

        public static HashSet<string> TextExtensions => new HashSet<string>(_textExtensions.Keys, StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> AudioExtensions => new HashSet<string>(_audioExtensions.Keys, StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> AllExtensions => new HashSet<string>(_textExtensions.Keys.Concat(_audioExtensions.Keys), StringComparer.OrdinalIgnoreCase);

        public static Quality GetQualityForExtension(string extension)
        {
            if (_textExtensions.ContainsKey(extension))
            {
                return _textExtensions[extension];
            }

            if (_audioExtensions.ContainsKey(extension))
            {
                return _audioExtensions[extension];
            }

            return Quality.Unknown;
        }
    }
}
