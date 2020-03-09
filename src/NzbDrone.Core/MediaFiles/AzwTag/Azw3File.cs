namespace NzbDrone.Core.MediaFiles.Azw
{
    public class Azw3File : AzwFile
    {
        private readonly MobiHeader _mobiHeader;

        public Azw3File(string path)
        : base(path)
        {
            if (Section_count > 0)
            {
                Sections = new Section[Section_count];
                if (Ident == "BOOKMOBI")
                {
                    _mobiHeader = new MobiHeader(GetSectionData(0));
                    Sections[0] = _mobiHeader;

                    if (_mobiHeader._codepage != 65001)
                    {
                        throw new AzwTagException("not UTF8");
                    }

                    if (_mobiHeader._version != 8 && _mobiHeader._version != 6)
                    {
                        throw new AzwTagException("Unhandled mobi version:" + _mobiHeader._version);
                    }
                }
            }
        }

        public string Title => _mobiHeader._title;
        public string Author => _mobiHeader.ExtMeta.StringOrNull(100);
        public string Isbn => _mobiHeader.ExtMeta.StringOrNull(104);
        public string Asin => _mobiHeader.ExtMeta.StringOrNull(113);
        public string PublishDate => _mobiHeader.ExtMeta.StringOrNull(106);
        public string Publisher => _mobiHeader.ExtMeta.StringOrNull(101);
        public string Language => _mobiHeader.ExtMeta.StringOrNull(524);
        public uint Version => _mobiHeader._version;
    }
}
