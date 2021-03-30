using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NzbDrone.Core.Books.Calibre
{
    public class CalibreDateConverter : IsoDateTimeConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
            {
                return null;
            }

            if (reader.Value as string == "None")
            {
                return null;
            }

            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
    }
}
