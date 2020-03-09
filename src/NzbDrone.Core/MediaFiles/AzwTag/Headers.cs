using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NzbDrone.Core.MediaFiles.Azw
{
    public class ExtMeta
    {
        public Dictionary<uint, ulong> IdValue;
        public Dictionary<uint, string> IdString;
        public Dictionary<uint, string> IdHex;

        public ExtMeta(byte[] ext)
        {
            IdValue = new Dictionary<uint, ulong>();
            IdString = new Dictionary<uint, string>();
            IdHex = new Dictionary<uint, string>();

            var len = Util.GetUInt32(ext, 4);
            var num_items = Util.GetUInt32(ext, 8);
            uint pos = 12;
            for (var i = 0; i < num_items; i++)
            {
                var id = Util.GetUInt32(ext, pos);
                var size = Util.GetUInt32(ext, pos + 4);
                if (IdMapping.Id_map_strings.ContainsKey(id))
                {
                    var a = Encoding.UTF8.GetString(Util.SubArray(ext, pos + 8, size - 8));

                    //Log.log(" " + IdMapping.id_map_strings[id] + ":" + a);
                    if (IdString.ContainsKey(id))
                    {
                        if (id == 100 || id == 517)
                        {
                            IdString[id] += "&" + a;
                        }
                        else
                        {
                            Console.WriteLine(string.Format("Meta id duplicate:{0}\nPervious:{1}  \nLatter:{2}", IdMapping.Id_map_strings[id], IdString[id], a));
                        }
                    }
                    else
                    {
                        IdString.Add(id, a);
                    }
                }
                else if (IdMapping.Id_map_values.ContainsKey(id))
                {
                    ulong a = 0;
                    switch (size)
                    {
                        case 9: a = Util.GetUInt8(ext, pos + 8); break;
                        case 10: a = Util.GetUInt16(ext, pos + 8); break;
                        case 12: a = Util.GetUInt32(ext, pos + 8); break;
                        case 16: a = Util.GetUInt64(ext, pos + 8); break;
                        default: Console.WriteLine("unexpected size:" + size); break;
                    }

                    // Console.WriteLine(" " + IdMapping.id_map_values[id] + ":" + a);
                    if (IdValue.ContainsKey(id))
                    {
                        Console.WriteLine(string.Format("Meta id duplicate:{0}\nPervious:{1}  \nLatter:{2}", IdMapping.Id_map_values[id], IdValue[id], a));
                    }
                    else
                    {
                        IdValue.Add(id, a);
                    }
                }
                else if (IdMapping.Id_map_hex.ContainsKey(id))
                {
                    var a = Util.ToHexString(ext, pos + 8, size - 8);

                    // Console.WriteLine(" " + IdMapping.id_map_hex[id] + ":" + a);
                    if (IdHex.ContainsKey(id))
                    {
                        Console.WriteLine(string.Format("Meta id duplicate:{0}\nPervious:{1}  \nLatter:{2}", IdMapping.Id_map_hex[id], IdHex[id], a));
                    }
                    else
                    {
                        IdHex.Add(id, a);
                    }
                }
                else
                {
                    var a = Util.ToHexString(ext, pos + 8, size - 8);
                    Console.WriteLine(" unknown id " + id + ":" + a);
                }

                pos += size;
            }
        }

        public string StringOrNull(uint key)
        {
            return IdString.TryGetValue(key, out var value) ? value : null;
        }
    }

    public class MobiHeader : Section
    {
        public string _title;
        public ushort _records;
        public ushort _compression;
        public ushort _crypto_type;
        public ushort _mobi_flags;
        public uint _length;
        public uint _mobi_type;
        public uint _codepage;
        public uint _unique_id;
        public uint _version;
        public uint _exth_flag;
        public uint _first_res_index;
        public uint _first_nontext_index;
        public uint _ncx_index;
        public uint _frag_index;
        public uint _skel_index;
        public uint _guide_index;
        public uint _fdst_start_index;
        public uint _fdst_count;
        public uint _mobi_version;
        public uint _mobi_length;
        public uint _huffman_start_index;
        public uint _huffman_count;

        public ExtMeta ExtMeta;

        public MobiHeader(byte[] header)
        : base("Mobi Header", header)
        {
            var mobi = Encoding.ASCII.GetString(header, 16, 4);
            _records = Util.GetUInt16(header, 8);
            _compression = Util.GetUInt16(header, 0);
            if (_compression == 0x4448)
            {
                _huffman_start_index = Util.GetUInt32(header, 0x70);
                _huffman_count = Util.GetUInt32(header, 0x74);
            }

            _length = Util.GetUInt32(header, 20);
            _mobi_type = Util.GetUInt32(header, 24);
            _codepage = Util.GetUInt32(header, 28);
            _unique_id = Util.GetUInt32(header, 32);
            _version = Util.GetUInt32(header, 36);
            _title = Encoding.UTF8.GetString(
                header,
                (int)Util.GetUInt32(header, 0x54),
                (int)Util.GetUInt32(header, 0x58));
            _exth_flag = Util.GetUInt32(header, 0x80);
            if ((_exth_flag & 0x40) > 0)
            {
                var exth = Util.SubArray(header, _length + 16, Util.GetUInt32(header, _length + 20));
                ExtMeta = new ExtMeta(exth);
            }

            _crypto_type = Util.GetUInt16(header, 0xc);
            if (_crypto_type != 0)
            {
                throw new AzwTagException("Unable to handle an encrypted file. Crypto Type:" + _crypto_type);
            }

            _first_res_index = Util.GetUInt32(header, 0x6c);
            _first_nontext_index = Util.GetUInt32(header, 0x50);
            _ncx_index = Util.GetUInt32(header, 0xf4);
            _skel_index = Util.GetUInt32(header, 0xfc);
            _frag_index = Util.GetUInt32(header, 0xf8);
            _guide_index = Util.GetUInt32(header, 0x104);
            _fdst_start_index = Util.GetUInt32(header, 0xc0);
            _fdst_count = Util.GetUInt32(header, 0xc4);
            _mobi_length = Util.GetUInt32(header, 0x14);
            _mobi_version = Util.GetUInt32(header, 0x68);
            _mobi_flags = Util.GetUInt16(header, 0xf2);
        }
    }

    public class Azw6Header : Section
    {
        public Azw6HeaderInfo Info;
        public ExtMeta Meta;
        public string Title;

        public Azw6Header(byte[] header_raw)
        : base("Azw6 Header", header_raw)
        {
            var header_size = Marshal.SizeOf(typeof(Azw6HeaderInfo));

            // Byte[] header_raw = Util.SubArray(azw6_data, section_info[0].start_addr, (ulong)header_size);
            Info = Util.GetStructBE<Azw6HeaderInfo>(header_raw, 0);
            Array.Reverse(Info.Magic);

            if (Info.Codepage != 65001)
            {
                return;
            }

            var title_raw = Util.SubArray(header_raw, Info.Title_offset, Info.Title_length);
            Title = Encoding.UTF8.GetString(title_raw);

            //Console.WriteLine("Azw6 File Title:" + title);
            var ext = Util.SubArray(header_raw, 48, header_raw.Length - 48);
            Meta = new ExtMeta(ext);
        }
    }
}
