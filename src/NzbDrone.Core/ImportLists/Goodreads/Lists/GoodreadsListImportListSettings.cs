using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.Goodreads
{
    public class GoodreadsListImportListValidator : AbstractValidator<GoodreadsListImportListSettings>
    {
        public GoodreadsListImportListValidator()
        {
            RuleFor(c => c.ListId).GreaterThan(0);
        }
    }

    public class GoodreadsListImportListSettings : IImportListSettings
    {
        private static readonly GoodreadsListImportListValidator Validator = new ();

        public GoodreadsListImportListSettings()
        {
            BaseUrl = "www.goodreads.com";
        }

        public string BaseUrl { get; set; }

        [FieldDefinition(0, Label = "List ID", HelpText = "Goodreads list ID")]
        public int ListId { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
