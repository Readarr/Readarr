using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Commands;
using NzbDrone.Core.Messaging.Commands;
using Readarr.Http;

namespace Readarr.Api.V1.Author
{
    [V1ApiController("author/editor")]
    public class AuthorEditorController : Controller
    {
        private readonly IAuthorService _authorService;
        private readonly IManageCommandQueue _commandQueueManager;

        public AuthorEditorController(IAuthorService authorService, IManageCommandQueue commandQueueManager)
        {
            _authorService = authorService;
            _commandQueueManager = commandQueueManager;
        }

        [HttpPut]
        public IActionResult SaveAll([FromBody] AuthorEditorResource resource)
        {
            var authorsToUpdate = _authorService.GetAuthors(resource.AuthorIds);
            var authorsToMove = new List<BulkMoveAuthor>();

            foreach (var author in authorsToUpdate)
            {
                if (resource.Monitored.HasValue)
                {
                    author.Monitored = resource.Monitored.Value;
                }

                if (resource.MonitorNewItems.HasValue)
                {
                    author.MonitorNewItems = resource.MonitorNewItems.Value;
                }

                if (resource.QualityProfileId.HasValue)
                {
                    author.QualityProfileId = resource.QualityProfileId.Value;
                }

                if (resource.MetadataProfileId.HasValue)
                {
                    author.MetadataProfileId = resource.MetadataProfileId.Value;
                }

                if (resource.RootFolderPath.IsNotNullOrWhiteSpace())
                {
                    author.RootFolderPath = resource.RootFolderPath;
                    authorsToMove.Add(new BulkMoveAuthor
                    {
                        AuthorId = author.Id,
                        SourcePath = author.Path
                    });
                }

                if (resource.Tags != null)
                {
                    var newTags = resource.Tags;
                    var applyTags = resource.ApplyTags;

                    switch (applyTags)
                    {
                        case ApplyTags.Add:
                            newTags.ForEach(t => author.Tags.Add(t));
                            break;
                        case ApplyTags.Remove:
                            newTags.ForEach(t => author.Tags.Remove(t));
                            break;
                        case ApplyTags.Replace:
                            author.Tags = new HashSet<int>(newTags);
                            break;
                    }
                }
            }

            if (resource.MoveFiles && authorsToMove.Any())
            {
                _commandQueueManager.Push(new BulkMoveAuthorCommand
                {
                    DestinationRootFolder = resource.RootFolderPath,
                    Author = authorsToMove
                });
            }

            return Accepted(_authorService.UpdateAuthors(authorsToUpdate, !resource.MoveFiles).ToResource());
        }

        [HttpDelete]
        public object DeleteAuthor([FromBody] AuthorEditorResource resource)
        {
            foreach (var authorId in resource.AuthorIds)
            {
                _authorService.DeleteAuthor(authorId, false);
            }

            return new { };
        }
    }
}
