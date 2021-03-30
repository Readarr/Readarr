using System;

namespace NzbDrone.Core.MediaFiles.Azw
{
    [Serializable]
    public class AzwTagException : Exception
    {
        public AzwTagException(string message)
            : base(message)
        {
        }

        protected AzwTagException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}
