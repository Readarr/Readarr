using System.Text;

namespace NzbDrone.Core.MediaFiles.Azw
{
    public class MobiHeader
    {
        public MobiHeader(byte[] header)
        {
            var mobi = Encoding.ASCII.GetString(header, 16, 4);
            if (mobi != "MOBI")
            {
                throw new AzwTagException("Invalid mobi header");
            }

            Version = Util.GetUInt32(header, 36);
            MobiType = Util.GetUInt32(header, 24);

            var codepage = Util.GetUInt32(header, 28);

            var encoding = codepage == 65001 ? Encoding.UTF8 : CodePagesEncodingProvider.Instance.GetEncoding((int)codepage);
            Title = encoding.GetString(header, (int)Util.GetUInt32(header, 0x54), (int)Util.GetUInt32(header, 0x58));

            var exthFlag = Util.GetUInt32(header, 0x80);
            var length = Util.GetUInt32(header, 20);
            if ((exthFlag & 0x40) > 0)
            {
                var exth = Util.SubArray(header, length + 16, Util.GetUInt32(header, length + 20));
                ExtMeta = new ExtMeta(exth, encoding);
            }
            else
            {
                throw new AzwTagException("No EXTH header. Readarr cannot process this file.");
            }
        }

        public string Title { get; }
        public uint Version { get; }
        public uint MobiType { get; }
        public ExtMeta ExtMeta { get; }
    }
}
