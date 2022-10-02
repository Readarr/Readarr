using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Kavita;

public class KavitaSettingsValidator : AbstractValidator<KavitaSettings>
{
    public KavitaSettingsValidator()
    {
        RuleFor(c => c.Host).ValidHost();
        RuleFor(c => c.Port).InclusiveBetween(1, 65535);
        RuleFor(c => c.ApiKey).NotEmpty();
    }
}

public class KavitaSettings : IProviderConfig
{
    private static readonly KavitaSettingsValidator Validator = new KavitaSettingsValidator();

    public KavitaSettings()
    {
        Port = 4040;
    }

    [FieldDefinition(0, Label = "Host")]
    public string Host { get; set; }

    [FieldDefinition(1, Label = "Port")]
    public int Port { get; set; }

    [FieldDefinition(2, Label = "API Key", Privacy = PrivacyLevel.ApiKey, HelpLink = "https://wiki.kavitareader.com/en/guides/settings/opds")]
    public string ApiKey { get; set; }

    [FieldDefinition(3, Label = "Use SSL", Type = FieldType.Checkbox, HelpText = "Connect to Kavita over HTTPS instead of HTTP")]
    public bool UseSsl { get; set; }

    [FieldDefinition(4, Label = "Update Library", Type = FieldType.Checkbox)]
    public bool Notify { get; set; }

    public NzbDroneValidationResult Validate()
    {
        return new NzbDroneValidationResult(Validator.Validate(this));
    }
}
