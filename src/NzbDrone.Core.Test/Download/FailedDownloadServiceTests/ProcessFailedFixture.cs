﻿using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Books;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Download.FailedDownloadServiceTests
{
    [TestFixture]
    public class ProcessFailedFixture : CoreTest<FailedDownloadService>
    {
        private TrackedDownload _trackedDownload;
        private List<EntityHistory> _grabHistory;

        [SetUp]
        public void Setup()
        {
            var completed = Builder<DownloadClientItem>.CreateNew()
                                                    .With(h => h.Status = DownloadItemStatus.Completed)
                                                    .With(h => h.OutputPath = new OsPath(@"C:\DropFolder\MyDownload".AsOsAgnostic()))
                                                    .With(h => h.Title = "Drone.S01E01.HDTV")
                                                    .Build();

            _grabHistory = Builder<EntityHistory>.CreateListOfSize(2).BuildList();

            var remoteBook = new RemoteBook
            {
                Author = new Author(),
                Books = new List<Book> { new Book { Id = 1 } }
            };

            _trackedDownload = Builder<TrackedDownload>.CreateNew()
                    .With(c => c.State = TrackedDownloadState.DownloadFailedPending)
                    .With(c => c.DownloadItem = completed)
                    .With(c => c.RemoteBook = remoteBook)
                    .Build();

            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.Find(_trackedDownload.DownloadItem.DownloadId, EntityHistoryEventType.Grabbed))
                  .Returns(_grabHistory);
        }

        [Test]
        public void should_mark_failed_if_encrypted()
        {
            _trackedDownload.DownloadItem.IsEncrypted = true;

            Subject.ProcessFailed(_trackedDownload);

            AssertDownloadFailed();
        }

        [Test]
        public void should_mark_failed_if_download_item_is_failed()
        {
            _trackedDownload.DownloadItem.Status = DownloadItemStatus.Failed;

            Subject.ProcessFailed(_trackedDownload);

            AssertDownloadFailed();
        }

        [Test]
        public void should_include_tracked_download_in_message()
        {
            _trackedDownload.DownloadItem.Status = DownloadItemStatus.Failed;

            Subject.ProcessFailed(_trackedDownload);

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.Is<DownloadFailedEvent>(c => c.TrackedDownload != null)), Times.Once());

            AssertDownloadFailed();
        }

        private void AssertDownloadNotFailed()
        {
            Mocker.GetMock<IEventAggregator>()
               .Verify(v => v.PublishEvent(It.IsAny<DownloadFailedEvent>()), Times.Never());

            _trackedDownload.State.Should().NotBe(TrackedDownloadState.DownloadFailed);
        }

        private void AssertDownloadFailed()
        {
            Mocker.GetMock<IEventAggregator>()
            .Verify(v => v.PublishEvent(It.IsAny<DownloadFailedEvent>()), Times.Once());

            _trackedDownload.State.Should().Be(TrackedDownloadState.DownloadFailed);
        }
    }
}
