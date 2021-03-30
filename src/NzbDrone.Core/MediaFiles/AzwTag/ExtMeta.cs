using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using NzbDrone.Common.Instrumentation;

namespace NzbDrone.Core.MediaFiles.Azw
{
    public class ExtMeta
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(ExtMeta));

        public Dictionary<uint, ulong> IdValue { get; } = new Dictionary<uint, ulong>();
        public Dictionary<uint, List<string>> IdString { get; } = new Dictionary<uint, List<string>>();
        public Dictionary<uint, string> IdHex { get; } = new Dictionary<uint, string>();

        public ExtMeta(byte[] ext, Encoding encoding)
        {
            var num_items = Util.GetUInt32(ext, 8);
            uint pos = 12;
            for (var i = 0; i < num_items; i++)
            {
                var id = Util.GetUInt32(ext, pos);
                var size = Util.GetUInt32(ext, pos + 4);
                if (IdMapping.Id_map_strings.ContainsKey(id))
                {
                    var a = encoding.GetString(Util.SubArray(ext, pos + 8, size - 8));

                    if (IdString.ContainsKey(id))
                    {
                        IdString[id].Add(a);
                    }
                    else
                    {
                        IdString.Add(id, new List<string> { a });
                    }
                }
                else if (IdMapping.Id_map_values.ContainsKey(id))
                {
                    ulong a = 0;
                    switch (size)
                    {
                        case 9:
                            a = Util.GetUInt8(ext, pos + 8);
                            break;
                        case 10:
                            a = Util.GetUInt16(ext, pos + 8);
                            break;
                        case 12:
                            a = Util.GetUInt32(ext, pos + 8);
                            break;
                        case 16:
                            a = Util.GetUInt64(ext, pos + 8);
                            break;
                        default:
                            Logger.Warn("unexpected size:" + size);
                            break;
                    }

                    if (IdValue.ContainsKey(id))
                    {
                        Logger.Debug("Meta id duplicate:{0}\nPervious:{1}  \nLatter:{2}", IdMapping.Id_map_values[id], IdValue[id], a);
                    }
                    else
                    {
                        IdValue.Add(id, a);
                    }
                }
                else if (IdMapping.Id_map_hex.ContainsKey(id))
                {
                    var a = Util.ToHexString(ext, pos + 8, size - 8);

                    if (IdHex.ContainsKey(id))
                    {
                        Logger.Debug("Meta id duplicate:{0}\nPervious:{1}  \nLatter:{2}", IdMapping.Id_map_hex[id], IdHex[id], a);
                    }
                    else
                    {
                        IdHex.Add(id, a);
                    }
                }
                else
                {
                    // Unknown id
                }

                pos += size;
            }
        }

        public string StringOrNull(uint key)
        {
            return IdString.TryGetValue(key, out var value) ? value.FirstOrDefault() : null;
        }

        public List<string> StringList(uint key)
        {
            return IdString.TryGetValue(key, out var value) ? value : new List<string>();
        }
    }
}
