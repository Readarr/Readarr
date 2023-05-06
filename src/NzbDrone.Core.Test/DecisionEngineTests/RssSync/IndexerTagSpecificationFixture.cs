﻿using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.DecisionEngine.Specifications.RssSync;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests.RssSync
{
    [TestFixture]
    public class IndexerTagSpecificationFixture : CoreTest<IndexerTagSpecification>
    {
        private IndexerTagSpecification _specification;

        private RemoteBook _parseResultMulti;
        private IndexerDefinition _fakeIndexerDefinition;
        private Author _fakeAuthor;
        private Book _firstBook;
        private Book _secondBook;
        private ReleaseInfo _fakeRelease;

        [SetUp]
        public void Setup()
        {
            _fakeIndexerDefinition = new IndexerDefinition
            {
                Tags = new HashSet<int>()
            };

            Mocker
                .GetMock<IIndexerRepository>()
                .Setup(m => m.Get(It.IsAny<int>()))
                .Returns(_fakeIndexerDefinition);

            _specification = Mocker.Resolve<IndexerTagSpecification>();

            _fakeAuthor = Builder<Author>.CreateNew()
                .With(c => c.Monitored = true)
                .With(c => c.Tags = new HashSet<int>())
                .Build();

            _fakeRelease = new ReleaseInfo
            {
                IndexerId = 1
            };

            _firstBook = new Book { Monitored = true };
            _secondBook = new Book { Monitored = true };

            var doubleBookList = new List<Book> { _firstBook, _secondBook };

            _parseResultMulti = new RemoteBook
            {
                Author = _fakeAuthor,
                Books = doubleBookList,
                Release = _fakeRelease
            };
        }

        [Test]
        public void indexer_and_author_without_tags_should_return_true()
        {
            _fakeIndexerDefinition.Tags = new HashSet<int>();
            _fakeAuthor.Tags = new HashSet<int>();

            _specification.IsSatisfiedBy(_parseResultMulti, new BookSearchCriteria { MonitoredBooksOnly = true }).Accepted.Should().BeTrue();
        }

        [Test]
        public void indexer_with_tags_author_without_tags_should_return_false()
        {
            _fakeIndexerDefinition.Tags = new HashSet<int> { 123 };
            _fakeAuthor.Tags = new HashSet<int>();

            _specification.IsSatisfiedBy(_parseResultMulti, new BookSearchCriteria { MonitoredBooksOnly = true }).Accepted.Should().BeFalse();
        }

        [Test]
        public void indexer_without_tags_author_with_tags_should_return_true()
        {
            _fakeIndexerDefinition.Tags = new HashSet<int>();
            _fakeAuthor.Tags = new HashSet<int> { 123 };

            _specification.IsSatisfiedBy(_parseResultMulti, new BookSearchCriteria { MonitoredBooksOnly = true }).Accepted.Should().BeTrue();
        }

        [Test]
        public void indexer_with_tags_author_with_matching_tags_should_return_true()
        {
            _fakeIndexerDefinition.Tags = new HashSet<int> { 123, 456 };
            _fakeAuthor.Tags = new HashSet<int> { 123, 789 };

            _specification.IsSatisfiedBy(_parseResultMulti, new BookSearchCriteria { MonitoredBooksOnly = true }).Accepted.Should().BeTrue();
        }

        [Test]
        public void indexer_with_tags_author_with_different_tags_should_return_false()
        {
            _fakeIndexerDefinition.Tags = new HashSet<int> { 456 };
            _fakeAuthor.Tags = new HashSet<int> { 123, 789 };

            _specification.IsSatisfiedBy(_parseResultMulti, new BookSearchCriteria { MonitoredBooksOnly = true }).Accepted.Should().BeFalse();
        }
    }
}
