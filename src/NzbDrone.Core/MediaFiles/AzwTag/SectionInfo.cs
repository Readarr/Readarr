namespace NzbDrone.Core.MediaFiles.Azw
{
    public struct SectionInfo
    {
        public ulong Start_addr;
        public ulong End_addr;

        public ulong Length => End_addr - Start_addr;
    }
}
