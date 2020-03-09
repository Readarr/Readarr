using System;
using System.Collections.Generic;
using System.Text;

namespace NzbDrone.Core.MediaFiles.Azw
{
    public interface ITextSectionDecoder
    {
        byte[] Decode(byte[] data);
    }

    public class PalmdocDecoder : ITextSectionDecoder
    {
        public byte[] Decode(byte[] data)
        {
            var r = new List<byte>();
            var pos = 0;
            while (pos < data.Length)
            {
                var c = data[pos];
                pos++;
                if (c >= 1 && c <= 8)
                {
                    r.AddRange(Util.SubArray(data, pos, c));
                    pos += c;
                }
                else if (c < 128)
                {
                    r.Add(c);
                }
                else if (c >= 192)
                {
                    r.Add(0x20);
                    r.Add((byte)(c ^ 128));
                }
                else
                {
                    if (pos < data.Length)
                    {
                        var cx = (c << 8) | data[pos];
                        pos++;
                        var m = (cx >> 3) & 0x07ff;
                        var n = (cx & 7) + 3;
                        if (m > n)
                        {
                            r.AddRange(Util.SubArray(r.ToArray(), r.Count - m, n));
                        }
                        else
                        {
                            for (var i = 0; i < n; i++)
                            {
                                if (m == 1)
                                {
                                    r.AddRange(Util.SubArray(r.ToArray(), r.Count - m, m));
                                }
                                else
                                {
                                    r.Add(r[r.Count - m]);
                                }
                            }
                        }
                    }
                }
            }

            return r.ToArray();
        }
    }

    public class HuffmanDecoder : ITextSectionDecoder
    {
        private readonly ulong[] _mincode = new ulong[33];
        private readonly ulong[] _maxcode = new ulong[33];
        private readonly uint[] _codelen = new uint[256];
        private readonly bool[] _term = new bool[256];
        private readonly uint[] _maxcode1 = new uint[256];
        private readonly HuffmanCDIC _cdic;

        public HuffmanDecoder(byte[] seed_section)
        {
            var ident = Encoding.ASCII.GetString(seed_section, 0, 4);
            if (ident != "HUFF")
            {
                throw new AzwTagException("Unexpect Section Header at Huff Decoder");
            }

            var off1 = Util.GetUInt32(seed_section, 8);
            var off2 = Util.GetUInt32(seed_section, 12);

            for (uint i = 0; i < 256; i++)
            {
                var v = Util.GetUInt32(seed_section, off1 + (i * 4));
                _codelen[i] = v & 0x1f;
                _term[i] = (v & 0x80) > 0;
                _maxcode1[i] = v >> 8;

                if (_codelen[i] == 0 || (_codelen[i] <= 8 && !_term[i]))
                {
                    throw new AzwTagException("Huff decode error.");
                }

                _maxcode1[i] = ((_maxcode1[i] + 1) << (int)(32 - _codelen[i])) - 1;
            }

            _mincode[0] = 0;
            _maxcode[0] = (1UL << 32) - 1;

            for (uint i = 1; i < 33; i++)
            {
                _mincode[i] = Util.GetUInt32(seed_section, off2 + ((i - 1) * 4 * 2));
                _maxcode[i] = Util.GetUInt32(seed_section, off2 + ((i - 1) * 4 * 2) + 4);
                _mincode[i] = _mincode[i] << (int)(32 - i);
                _maxcode[i] = ((_maxcode[i] + 1) << (int)(32 - i)) - 1;
            }

            _cdic = new HuffmanCDIC();
        }

        public byte[] Decode(byte[] indata)
        {
            var data = new byte[indata.Length + 8];
            indata.CopyTo(data, 0);

            long bitsleft = indata.Length * 8;
            ulong pos = 0;
            var x = Util.GetUInt64(data, pos);
            var n = 32;
            var s = new List<byte>();
            while (true)
            {
                if (n <= 0)
                {
                    pos += 4;
                    x = Util.GetUInt64(data, pos);
                    n += 32;
                }

                var code = (x >> n) & ((1UL << 32) - 1);
                var dict1_i = code >> 24;
                var codelen = _codelen[dict1_i];
                ulong maxcode = _maxcode1[dict1_i];

                if (!_term[dict1_i])
                {
                    while (code < _mincode[codelen])
                    {
                        codelen++;
                    }

                    maxcode = _maxcode[codelen];
                }

                n -= (int)codelen;
                bitsleft -= codelen;

                if (bitsleft < 0)
                {
                    break;
                }

                var r = (maxcode - code) >> (int)(32 - codelen);
                var slice = _cdic._slice[(int)r];
                var flag = _cdic._slice_flag[(int)r];

                if (!flag)
                {
                    _cdic._slice[(int)r] = new byte[0];
                    slice = Decode(slice);
                    _cdic._slice[(int)r] = slice;
                    _cdic._slice_flag[(int)r] = true;
                }

                s.AddRange(slice);
            }

            return s.ToArray();
        }
    }

    public class HuffmanCDIC
    {
        public List<byte[]> _slice = new List<byte[]>();
        public List<bool> _slice_flag = new List<bool>();

        public void Add(byte[] raw)
        {
            var ident = Encoding.ASCII.GetString(raw, 0, 4);
            if (ident != "CDIC")
            {
                throw new AzwTagException("Unexpect Section Header at CDIC");
            }

            var phases = Util.GetUInt32(raw, 8);
            var bits = Util.GetUInt32(raw, 12);
            var n = Math.Min(1 << (int)bits, phases - _slice.Count);

            for (var i = 0; i < n; i++)
            {
                var off = Util.GetUInt16(raw, (ulong)(16 + (i * 2)));
                var length = Util.GetUInt16(raw, (ulong)(16 + off));
                _slice_flag.Add((length & 0x8000) > 0);
                _slice.Add(Util.SubArray(raw, (ulong)(18 + off), (ulong)(length & 0x7fff)));
            }
        }
    }

    public class HuffmanCDIC_Section : Section
    {
        private readonly int _size;

        public HuffmanCDIC_Section(byte[] r)
        : base("Huffman CDIC", null)
        {
            _size = r.Length;
        }

        public override int GetSize()
        {
            return _size;
        }
    }

    public class Huffman_Section : Section
    {
        private readonly int _size;

        public Huffman_Section(byte[] r)
        : base("Huffman", null)
        {
            _size = r.Length;
        }

        public override int GetSize()
        {
            return _size;
        }
    }
}
