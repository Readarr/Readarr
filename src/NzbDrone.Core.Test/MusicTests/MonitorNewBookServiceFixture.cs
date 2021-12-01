using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.BookTests
{
    [TestFixture]
    public class MonitorNewBookServiceFixture : CoreTest<MonitorNewBookService>
    {
        private List<Book> _books;

        [SetUp]
        public void Setup()
        {
            _books = Builder<Book>.CreateListOfSize(4)
                .All()
                .With(e => e.Monitored = true)
                .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(-7))

                //Future
                .TheFirst(1)
                .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(7))

                //Future/TBA
                .TheNext(1)
                .With(e => e.ReleaseDate = null)
                .Build()
                .ToList();
        }

        [Test]
        public void should_monitor_with_all()
        {
            foreach (var book in _books)
            {
                Subject.ShouldMonitorNewBook(book, _books, NewItemMonitorTypes.All).Should().BeTrue();
            }
        }

        [Test]
        public void should_not_monitor_with_none()
        {
            foreach (var book in _books)
            {
                Subject.ShouldMonitorNewBook(book, _books, NewItemMonitorTypes.None).Should().BeFalse();
            }
        }

        [Test]
        public void should_only_monitor_new_with_new()
        {
            Subject.ShouldMonitorNewBook(_books[0], _books, NewItemMonitorTypes.New).Should().BeTrue();

            foreach (var book in _books.Skip(1))
            {
                Subject.ShouldMonitorNewBook(book, _books, NewItemMonitorTypes.New).Should().BeFalse();
            }
        }
    }
}
