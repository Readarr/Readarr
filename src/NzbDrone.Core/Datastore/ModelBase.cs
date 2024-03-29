using System.Diagnostics;

namespace NzbDrone.Core.Datastore
{
    [DebuggerDisplay("{GetType().FullName} ID = {Id}")]
    public abstract class ModelBase
    {
        public int Id { get; set; }
    }
}
