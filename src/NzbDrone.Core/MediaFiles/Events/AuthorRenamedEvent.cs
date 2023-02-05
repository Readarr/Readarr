using System.Collections.Generic;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class AuthorRenamedEvent : IEvent
    {
        public Author Author { get; private set; }
        public List<RenamedBookFile> RenamedFiles { get; private set; }

        public AuthorRenamedEvent(Author author, List<RenamedBookFile> renamedFiles)
        {
            Author = author;
            RenamedFiles = renamedFiles;
        }
    }
}
