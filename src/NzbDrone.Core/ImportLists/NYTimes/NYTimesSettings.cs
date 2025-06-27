using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.NYTimes
{
    public class NYTimesSettingsValidator : AbstractValidator<NYTimesSettings>
    {
        public NYTimesSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty();
            RuleFor(c => c.ListName).NotEmpty();
        }
    }

    public class NYTimesSettings : IImportListSettings
    {
        private static readonly NYTimesSettingsValidator Validator = new ();
        public NYTimesSettings()
        {
            BaseUrl = "https://api.nytimes.com/svc/books/v3";
        }

        public string BaseUrl { get; set; }

        [FieldDefinition(0, Label = "API Key")]
        public string ApiKey { get; set; }

        [FieldDefinition(1, Type = FieldType.Select, SelectOptionsProviderAction = "getNames", Label = "List Type", HelpText = "The name of the Times best sellers list")]
        public string ListName { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
