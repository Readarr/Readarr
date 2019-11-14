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
        {
            _fileNameBuilder = fileNameBuilder;
        }

        protected override string GetDefaultMessageTemplate() => "Root folder path '{rootFolderPath}' contains author folder '{authorFolder}'";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            if (context.ParentContext.InstanceToValidate is not AuthorResource authorResource)
            {
                return true;
            }

            var rootFolderPath = context.PropertyValue.ToString();

            if (rootFolderPath.IsNullOrWhiteSpace())
            {
                return true;
            }

            var rootFolder = new DirectoryInfo(rootFolderPath!).Name;
            var author = authorResource.ToModel();
            var authorFolder = _fileNameBuilder.GetAuthorFolder(author);

            context.MessageFormatter.AppendArgument("rootFolderPath", rootFolderPath);
            context.MessageFormatter.AppendArgument("authorFolder", authorFolder);

            if (authorFolder == rootFolder)
            {
                return false;
            }

            var distance = authorFolder.LevenshteinDistance(rootFolder);

            return distance >= Math.Max(1, authorFolder.Length * 0.2);
        }
    }
}
