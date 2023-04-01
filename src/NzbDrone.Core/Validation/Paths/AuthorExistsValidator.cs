using FluentValidation.Validators;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.Validation.Paths
{
    public class AuthorExistsValidator : PropertyValidator
    {
        private readonly IAuthorService _authorService;

        public AuthorExistsValidator(IAuthorService authorService)
        {
            _authorService = authorService;
        }

        protected override string GetDefaultMessageTemplate() => "This author has already been added";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            return !_authorService.GetAllAuthors().Exists(s => s.Metadata.Value.ForeignAuthorId == context.PropertyValue.ToString());
        }
    }
}
