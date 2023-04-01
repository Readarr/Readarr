using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace NzbDrone.Core.Books
{
    public interface IMonitorNewBookService
    {
        bool ShouldMonitorNewBook(Book addedBook, List<Book> existingBooks, NewItemMonitorTypes author);
    }

    public class MonitorNewBookService : IMonitorNewBookService
    {
        private readonly Logger _logger;

        public MonitorNewBookService(Logger logger)
        {
            _logger = logger;
        }

        public bool ShouldMonitorNewBook(Book addedBook, List<Book> existingBooks, NewItemMonitorTypes monitorNewItems)
        {
            if (monitorNewItems == NewItemMonitorTypes.None)
            {
                return false;
            }

            if (monitorNewItems == NewItemMonitorTypes.All)
            {
                return true;
            }

            if (monitorNewItems == NewItemMonitorTypes.New)
            {
                var newest = existingBooks.MaxBy(x => x.ReleaseDate ?? DateTime.MinValue)?.ReleaseDate ?? DateTime.MinValue;

                return (addedBook.ReleaseDate ?? DateTime.MinValue) >= newest;
            }

            throw new NotImplementedException($"Unknown new item monitor type {monitorNewItems}");
        }
    }
}
