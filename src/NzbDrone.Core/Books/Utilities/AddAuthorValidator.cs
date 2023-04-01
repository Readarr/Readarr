using FluentValidation;
using FluentValidation.Results;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;

namespace NzbDrone.Core.Books
{
    public interface IAddAuthorValidator
    {
        ValidationResult Validate(Author instance);
    }

    public class AddAuthorValidator : AbstractValidator<Author>, IAddAuthorValidator
    {
        public AddAuthorValidator(RootFolderValidator rootFolderValidator,
                                  RecycleBinValidator recycleBinValidator,
                                  AuthorPathValidator authorPathValidator,
                                  AuthorAncestorValidator authorAncestorValidator,
                                  QualityProfileExistsValidator qualityProfileExistsValidator,
                                  MetadataProfileExistsValidator metadataProfileExistsValidator)
        {
            RuleFor(c => c.Path).Cascade(CascadeMode.Stop)
                                .IsValidPath()
                                .SetValidator(rootFolderValidator)
                                .SetValidator(recycleBinValidator)
                                .SetValidator(authorPathValidator)
                                .SetValidator(authorAncestorValidator);

            RuleFor(c => c.QualityProfileId).SetValidator(qualityProfileExistsValidator);

            RuleFor(c => c.MetadataProfileId).SetValidator(metadataProfileExistsValidator);
        }
    }
}
