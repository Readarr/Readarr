using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Calibre
{
    public class CalibreSettingsValidator : AbstractValidator<CalibreSettings>
    {
        public CalibreSettingsValidator()
        {
            RuleFor(c => c.Url).IsValidUrl();
            RuleFor(c => c.Username).NotEmpty().WithMessage("Calibre must have a user/password set and be run with --enable-auth");
            RuleFor(c => c.Password).NotEmpty().WithMessage("Calibre must have a user/password set and be run with --enable-auth");
        }
    }

    public class CalibreSettings : IProviderConfig
    {
        private static readonly CalibreSettingsValidator Validator = new CalibreSettingsValidator();

        [FieldDefinition(0, Label = "URL", Type = FieldType.Url)]
        public string Url { get; set; }

        [FieldDefinition(1, Label = "Username", Type = FieldType.Textbox)]
        public string Username { get; set; }

        [FieldDefinition(2, Label = "Password", Type = FieldType.Password)]
        public string Password { get; set; }

        [FieldDefinition(3, Label = "Convert to Format", Type = FieldType.Select, SelectOptions = typeof(CalibreFormat), HelpText = "Optionally ask calibre to convert to another format on import")]
        public int OutputFormat { get; set; }

        [FieldDefinition(3, Label = "Conversion Profile", Type = FieldType.Select, SelectOptions = typeof(CalibreProfile), HelpText = "The output profile to use for conversion")]
        public int OutputProfile { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
