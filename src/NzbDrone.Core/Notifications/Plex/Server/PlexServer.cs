using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Notifications.Plex.PlexTv;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Plex.Server
{
    public class PlexServer : NotificationBase<PlexServerSettings>
    {
        private readonly IPlexServerService _plexServerService;
        private readonly IPlexTvService _plexTvService;
        private readonly Logger _logger;

        private class PlexUpdateQueue
        {
            public Dictionary<int, Author> Pending { get; } = new ();
            public bool Refreshing { get; set; }
        }

        private readonly ICached<PlexUpdateQueue> _pendingAuthorsCache;

        public PlexServer(IPlexServerService plexServerService, IPlexTvService plexTvService, ICacheManager cacheManager, Logger logger)
        {
            _plexServerService = plexServerService;
            _plexTvService = plexTvService;
            _logger = logger;

            _pendingAuthorsCache = cacheManager.GetRollingCache<PlexUpdateQueue>(GetType(), "pendingAuthors", TimeSpan.FromDays(1));
        }

        public override string Link => "https://www.plex.tv/";
        public override string Name => "Plex Media Server";

        public override void OnReleaseImport(BookDownloadMessage message)
        {
            UpdateIfEnabled(message.Author);
        }

        public override void OnRename(Author author, List<RenamedBookFile> renamedFiles)
        {
            UpdateIfEnabled(author);
        }

        public override void OnBookRetag(BookRetagMessage message)
        {
            UpdateIfEnabled(message.Author);
        }

        public override void OnBookDelete(BookDeleteMessage deleteMessage)
        {
            if (deleteMessage.DeletedFiles)
            {
                UpdateIfEnabled(deleteMessage.Book.Author);
            }
        }

        public override void OnAuthorDelete(AuthorDeleteMessage deleteMessage)
        {
            if (deleteMessage.DeletedFiles)
            {
                UpdateIfEnabled(deleteMessage.Author);
            }
        }

        private void UpdateIfEnabled(Author author)
        {
            _plexTvService.Ping(Settings.AuthToken);

            if (Settings.UpdateLibrary)
            {
                _logger.Debug("Scheduling library update for author {0} {1}", author.Id, author.Name);
                var queue = _pendingAuthorsCache.Get(Settings.Host, () => new PlexUpdateQueue());
                lock (queue)
                {
                    queue.Pending[author.Id] = author;
                }
            }
        }

        public override void ProcessQueue()
        {
            var queue = _pendingAuthorsCache.Find(Settings.Host);

            if (queue == null)
            {
                return;
            }

            lock (queue)
            {
                if (queue.Refreshing)
                {
                    return;
                }

                queue.Refreshing = true;
            }

            try
            {
                while (true)
                {
                    List<Author> refreshingAuthors;
                    lock (queue)
                    {
                        if (queue.Pending.Empty())
                        {
                            queue.Refreshing = false;
                            return;
                        }

                        refreshingAuthors = queue.Pending.Values.ToList();
                        queue.Pending.Clear();
                    }

                    if (Settings.UpdateLibrary)
                    {
                        _logger.Debug("Performing library update for {0} authors", refreshingAuthors.Count);
                        _plexServerService.UpdateLibrary(refreshingAuthors, Settings);
                    }
                }
            }
            catch
            {
                lock (queue)
                {
                    queue.Refreshing = false;
                }

                throw;
            }
        }

        public override ValidationResult Test()
        {
            _plexTvService.Ping(Settings.AuthToken);

            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_plexServerService.Test(Settings));

            return new ValidationResult(failures);
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "startOAuth")
            {
                Settings.Validate().Filter("ConsumerKey", "ConsumerSecret").ThrowOnError();

                return _plexTvService.GetPinUrl();
            }
            else if (action == "continueOAuth")
            {
                Settings.Validate().Filter("ConsumerKey", "ConsumerSecret").ThrowOnError();

                if (query["callbackUrl"].IsNullOrWhiteSpace())
                {
                    throw new BadRequestException("QueryParam callbackUrl invalid.");
                }

                if (query["id"].IsNullOrWhiteSpace())
                {
                    throw new BadRequestException("QueryParam id invalid.");
                }

                if (query["code"].IsNullOrWhiteSpace())
                {
                    throw new BadRequestException("QueryParam code invalid.");
                }

                return _plexTvService.GetSignInUrl(query["callbackUrl"], Convert.ToInt32(query["id"]), query["code"]);
            }
            else if (action == "getOAuthToken")
            {
                Settings.Validate().Filter("ConsumerKey", "ConsumerSecret").ThrowOnError();

                if (query["pinId"].IsNullOrWhiteSpace())
                {
                    throw new BadRequestException("QueryParam pinId invalid.");
                }

                var authToken = _plexTvService.GetAuthToken(Convert.ToInt32(query["pinId"]));

                return new
                       {
                           authToken
                       };
            }

            return new { };
        }
    }
}
