using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Books.Calibre
{
    public class CalibreSettingsValidator : AbstractValidator<CalibreSettings>
    {
        public CalibreSettingsValidator()
        {
            RuleFor(c => c.Host).IsValidUrl();
            RuleFor(c => c.Username).NotEmpty().WithMessage("Calibre must have a user/password set and be run with --enable-auth");
            RuleFor(c => c.Password).NotEmpty().WithMessage("Calibre must have a user/password set and be run with --enable-auth");
        }
    }

    public class CalibreSettings : IProviderConfig
    {
        private static readonly CalibreSettingsValidator Validator = new CalibreSettingsValidator();

        [FieldDefinition(0, Label = "Host", Type = FieldType.Textbox)]
        public string Host { get; set; }

        [FieldDefinition(0, Label = "Port", Type = FieldType.Textbox)]
        public int Port { get; set; }

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
