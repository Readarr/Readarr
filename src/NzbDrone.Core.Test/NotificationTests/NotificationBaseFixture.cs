using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.NotificationTests
{
    [TestFixture]
    public class NotificationBaseFixture : TestBase
    {
        private class TestSetting : IProviderConfig
        {
            public NzbDroneValidationResult Validate()
            {
                return new NzbDroneValidationResult();
            }
        }

        private class TestNotificationWithOnReleaseImport : NotificationBase<TestSetting>
        {
            public override string Name => "TestNotification";
            public override string Link => "";

            public override ValidationResult Test()
            {
                throw new NotImplementedException();
            }

            public override void OnReleaseImport(BookDownloadMessage message)
            {
                TestLogger.Info("OnDownload was called");
            }
        }

        private class TestNotificationWithAllEvents : NotificationBase<TestSetting>
        {
            public override string Name => "TestNotification";
            public override string Link => "";

            public override ValidationResult Test()
            {
                throw new NotImplementedException();
            }

            public override void OnGrab(GrabMessage grabMessage)
            {
                TestLogger.Info("OnGrab was called");
            }

            public override void OnReleaseImport(BookDownloadMessage message)
            {
                TestLogger.Info("OnDownload was called");
            }

            public override void OnRename(Author author, List<RenamedBookFile> renamedFiles)
            {
                TestLogger.Info("OnRename was called");
            }

            public override void OnAuthorAdded(Author author)
            {
                TestLogger.Info("OnAuthorAdded was called");
            }

            public override void OnAuthorDelete(AuthorDeleteMessage message)
            {
                TestLogger.Info("OnAuthorDelete was called");
            }

            public override void OnBookDelete(BookDeleteMessage message)
            {
                TestLogger.Info("OnBookDelete was called");
            }

            public override void OnBookFileDelete(BookFileDeleteMessage message)
            {
                TestLogger.Info("OnBookFileDelete was called");
            }

            public override void OnHealthIssue(NzbDrone.Core.HealthCheck.HealthCheck author)
            {
                TestLogger.Info("OnHealthIssue was called");
            }

            public override void OnDownloadFailure(DownloadFailedMessage message)
            {
                TestLogger.Info("OnDownloadFailure was called");
            }

            public override void OnImportFailure(BookDownloadMessage message)
            {
                TestLogger.Info("OnImportFailure was called");
            }

            public override void OnBookRetag(BookRetagMessage message)
            {
                TestLogger.Info("OnBookRetag was called");
            }

            public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
            {
                TestLogger.Info("OnApplicationUpdate was called");
            }
        }

        private class TestNotificationWithNoEvents : NotificationBase<TestSetting>
        {
            public override string Name => "TestNotification";
            public override string Link => "";

            public override ValidationResult Test()
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void should_support_OnUpgrade_should_link_to_OnReleaseImport()
        {
            var notification = new TestNotificationWithOnReleaseImport();

            notification.SupportsOnReleaseImport.Should().BeTrue();
            notification.SupportsOnUpgrade.Should().BeTrue();

            notification.SupportsOnGrab.Should().BeFalse();
            notification.SupportsOnRename.Should().BeFalse();
        }

        [Test]
        public void should_support_all_if_implemented()
        {
            var notification = new TestNotificationWithAllEvents();

            notification.SupportsOnGrab.Should().BeTrue();
            notification.SupportsOnReleaseImport.Should().BeTrue();
            notification.SupportsOnUpgrade.Should().BeTrue();
            notification.SupportsOnRename.Should().BeTrue();
            notification.SupportsOnHealthIssue.Should().BeTrue();
            notification.SupportsOnAuthorAdded.Should().BeTrue();
            notification.SupportsOnAuthorDelete.Should().BeTrue();
            notification.SupportsOnBookDelete.Should().BeTrue();
            notification.SupportsOnBookFileDelete.Should().BeTrue();
            notification.SupportsOnBookFileDeleteForUpgrade.Should().BeTrue();
            notification.SupportsOnDownloadFailure.Should().BeTrue();
            notification.SupportsOnImportFailure.Should().BeTrue();
            notification.SupportsOnBookRetag.Should().BeTrue();
            notification.SupportsOnApplicationUpdate.Should().BeTrue();
        }

        [Test]
        public void should_support_none_if_none_are_implemented()
        {
            var notification = new TestNotificationWithNoEvents();

            notification.SupportsOnGrab.Should().BeFalse();
            notification.SupportsOnReleaseImport.Should().BeFalse();
            notification.SupportsOnUpgrade.Should().BeFalse();
            notification.SupportsOnRename.Should().BeFalse();
            notification.SupportsOnAuthorAdded.Should().BeFalse();
            notification.SupportsOnAuthorDelete.Should().BeFalse();
            notification.SupportsOnBookDelete.Should().BeFalse();
            notification.SupportsOnBookFileDelete.Should().BeFalse();
            notification.SupportsOnBookFileDeleteForUpgrade.Should().BeFalse();
            notification.SupportsOnHealthIssue.Should().BeFalse();
            notification.SupportsOnDownloadFailure.Should().BeFalse();
            notification.SupportsOnImportFailure.Should().BeFalse();
            notification.SupportsOnBookRetag.Should().BeFalse();
            notification.SupportsOnApplicationUpdate.Should().BeFalse();
        }
    }
}
