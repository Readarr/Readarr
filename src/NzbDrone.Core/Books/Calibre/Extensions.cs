using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Books.Calibre
{
    public static class Extensions
    {
        private static readonly Dictionary<string, string> TwoToThree;
        private static readonly Dictionary<string, string> ByThree;
        private static readonly Dictionary<string, string> NameMap;

        // generated from https://www.loc.gov/standards/iso639-2/ISO-639-2_utf-8.txt
        public static readonly Dictionary<string, string> BtoTmap = new ()
        {
            { "alb", "sqi" },
            { "arm", "hye" },
            { "baq", "eus" },
            { "bur", "mya" },
            { "chi", "zho" },
            { "cze", "ces" },
            { "dut", "nld" },
            { "fre", "fra" },
            { "geo", "kat" },
            { "ger", "deu" },
            { "gre", "ell" },
            { "ice", "isl" },
            { "mac", "mkd" },
            { "mao", "mri" },
            { "may", "msa" },
            { "per", "fas" },
            { "rum", "ron" },
            { "slo", "slk" },
            { "tib", "bod" },
            { "wel", "cym" }
        };

        public static HashSet<string> KnownLanguages { get; }

        static Extensions()
        {
            var assembly = Assembly.GetExecutingAssembly();
            TwoToThree = InitializeDictionary(assembly, "2to3.json");
            ByThree = InitializeDictionary(assembly, "by3.json");
            NameMap = InitializeDictionary(assembly, "name_map.json");

            KnownLanguages = ByThree.Keys.ToHashSet();
        }

        private static Dictionary<string, string> InitializeDictionary(Assembly assembly, string resource)
        {
            var resources = assembly.GetManifestResourceNames();
            var stream = assembly.GetManifestResourceStream(resources.Single(x => x.EndsWith(resource)));

            string data;
            using (var reader = new StreamReader(stream))
            {
                data = reader.ReadToEnd();
            }

            return JsonSerializer.Deserialize<Dictionary<string, string>>(data);
        }

        // Translated from https://github.com/kovidgoyal/calibre/blob/ba06b7452228cfde9114e4735fb8d5785fba4955/src/calibre/utils/localization.py#L430
        public static string CanonicalizeLanguage(this string raw)
        {
            if (raw.IsNullOrWhiteSpace())
            {
                return null;
            }

            raw = raw.ToLowerInvariant().Trim();

            if (raw.IsNullOrWhiteSpace())
            {
                return null;
            }

            raw = raw.Replace('_', '-').Split('-', 2)[0].Trim();

            if (raw.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (raw.Length == 2)
            {
                if (TwoToThree.TryGetValue(raw, out var lang))
                {
                    return lang;
                }
            }
            else if (raw.Length == 3)
            {
                if (ByThree.ContainsKey(raw))
                {
                    return raw;
                }

                if (BtoTmap.TryGetValue(raw, out var mapped))
                {
                    return mapped;
                }
            }

            return NameMap.TryGetValue(raw, out var langByName) ? langByName : null;
        }
    }
}
