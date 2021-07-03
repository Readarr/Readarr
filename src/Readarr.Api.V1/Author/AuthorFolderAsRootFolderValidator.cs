using System;
using System.IO;
using FluentValidation.Validators;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Organizer;

namespace Readarr.Api.V1.Author
{
    public class AuthorFolderAsRootFolderValidator : PropertyValidator
    {
        private readonly IBuildFileNames _fileNameBuilder;

        public AuthorFolderAsRootFolderValidator(IBuildFileNames fileNameBuilder)
            : base("Root folder path contains author folder")
        {
            _fileNameBuilder = fileNameBuilder;
        }

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            var authorResource = context.Instance as AuthorResource;

            if (authorResource == null)
            {
                return true;
            }

            var rootFolderPath = context.PropertyValue.ToString();
            var rootFolder = new DirectoryInfo(rootFolderPath).Name;
            var author = authorResource.ToModel();
            var authorFolder = _fileNameBuilder.GetAuthorFolder(author);

            if (authorFolder == rootFolder)
            {
                return false;
            }

            var distance = authorFolder.LevenshteinDistance(rootFolder);

            return distance >= Math.Max(1, authorFolder.Length * 0.2);
        }
    }
}
