using System;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles.TrackImport.Manual
{
    public class ManualImportFile : IEquatable<ManualImportFile>
    {
        public string Path { get; set; }
        public int AuthorId { get; set; }
        public int BookId { get; set; }
        public QualityModel Quality { get; set; }
        public string DownloadId { get; set; }

        public bool Equals(ManualImportFile other)
        {
            if (other == null)
            {
                return false;
            }

            return Path.PathEquals(other.Path);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Path.PathEquals(((ManualImportFile)obj).Path);
        }

        public override int GetHashCode()
        {
            return Path != null ? Path.GetHashCode() : 0;
        }
    }
}
