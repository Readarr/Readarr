using System;
using System.IO;
using System.Text;

namespace NzbDrone.Core.MediaFiles.Azw
{
    public class AzwFile
    {
        public byte[] RawData { get; }
        public ushort SectionCount { get; private set; }
        public SectionInfo[] Info { get; private set; }
        public string Ident { get; private set; }

        protected AzwFile(string path)
        {
            RawData = File.ReadAllBytes(path);
            GetSectionInfo();
        }

        protected void GetSectionInfo()
        {
            Ident = Encoding.ASCII.GetString(RawData, 0x3c, 8);
            SectionCount = Math.Min(Util.GetUInt16(RawData, 76), (ushort)1);

            if (Ident != "BOOKMOBI" || SectionCount == 0)
            {
                throw new AzwTagException("Invalid mobi header");
            }

            Info = new SectionInfo[SectionCount];
            Info[0].Start_addr = Util.GetUInt32(RawData, 78);
            Info[0].End_addr = Util.GetUInt32(RawData, 78 + 8);
        }

        protected byte[] GetSectionData(uint i)
        {
            return Util.SubArray(RawData, Info[i].Start_addr, Info[i].Length);
        }
    }
}
