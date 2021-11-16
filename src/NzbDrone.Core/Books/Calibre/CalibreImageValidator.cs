using System;
using System.Linq;
using System.Text;

namespace NzbDrone.Core.Books.Calibre
{
    // see https://github.com/kovidgoyal/calibre/blob/876290c600b26d6ce915c5449c641d98053b9037/src/calibre/utils/imghdr.py#L16
    public static class CalibreImageValidator
    {
        private static readonly byte[] JFIF = Encoding.ASCII.GetBytes("JFIF");
        private static readonly byte[] EXIF = Encoding.ASCII.GetBytes("Exif");
        private static readonly byte[] BIM = Encoding.ASCII.GetBytes("8BIM");
        private static readonly byte[] JPEG_MARKER = new byte[] { 255, 216 };

        private static readonly byte[] GIF87 = Encoding.ASCII.GetBytes("GIF87a");
        private static readonly byte[] GIF89 = Encoding.ASCII.GetBytes("GIF89a");

        private static readonly byte[] PNG = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10  };

        public static bool IsValidImage(byte[] data)
        {
            //jpeg
            var jpegRange = data.Skip(6).Take(4);

            if (jpegRange.SequenceEqual(JFIF) ||
                jpegRange.SequenceEqual(EXIF))
            {
                return true;
            }

            if (data.Take(2).SequenceEqual(JPEG_MARKER))
            {
                for (var i = 0; i < 28; i++)
                {
                    var sub = data.Skip(i).Take(4);
                    if (sub.SequenceEqual(JFIF) || sub.SequenceEqual(BIM))
                    {
                        return true;
                    }
                }
            }

            // png
            if (data.Take(8).SequenceEqual(PNG))
            {
                return true;
            }

            // gif (not supported by calibre)
            var gifRange = data.Take(6);
            if (gifRange.SequenceEqual(GIF87) ||
                gifRange.SequenceEqual(GIF89))
            {
                return false;
            }

            // last ditch jpeg
            if (data.Take(2).SequenceEqual(JPEG_MARKER))
            {
                return true;
            }

            return false;
        }
    }
}
