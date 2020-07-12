using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Goodreads
{
    public class GoodreadsSettingsBaseValidator<TSettings> : AbstractValidator<TSettings>
        where TSettings : GoodreadsSettingsBase<TSettings>
    {
        public GoodreadsSettingsBaseValidator()
        {
            RuleFor(c => c.AccessToken).NotEmpty();
            RuleFor(c => c.AccessTokenSecret).NotEmpty();
        }
    }

    public abstract class GoodreadsSettingsBase<TSettings> : IProviderConfig
    where TSettings : GoodreadsSettingsBase<TSettings>
    {
        public GoodreadsSettingsBase()
        {
            SignIn = "startOAuth";
        }

        public string SigningUrl => "https://auth.servarr.com/v1/goodreads/sign";
        public string OAuthUrl => "https://www.goodreads.com/oauth/authorize";
        public string OAuthRequestTokenUrl => "https://www.goodreads.com/oauth/request_token";
        public string OAuthAccessTokenUrl => "https://www.goodreads.com/oauth/access_token";

        [FieldDefinition(0, Label = "Access Token", Type = FieldType.Textbox, Hidden = HiddenType.Hidden)]
        public string AccessToken { get; set; }

        [FieldDefinition(0, Label = "Access Token Secret", Type = FieldType.Textbox, Hidden = HiddenType.Hidden)]
        public string AccessTokenSecret { get; set; }

        [FieldDefinition(0, Label = "Request Token Secret", Type = FieldType.Textbox, Hidden = HiddenType.Hidden)]
        public string RequestTokenSecret { get; set; }

        [FieldDefinition(0, Label = "User Id", Type = FieldType.Textbox, Hidden = HiddenType.Hidden)]
        public string UserId { get; set; }

        [FieldDefinition(0, Label = "User Name", Type = FieldType.Textbox, Hidden = HiddenType.Hidden)]
        public string UserName { get; set; }

        [FieldDefinition(99, Label = "Authenticate with Goodreads", Type = FieldType.OAuth)]
        public string SignIn { get; set; }

        public bool IsValid => !string.IsNullOrWhiteSpace(AccessTokenSecret);

        public abstract NzbDroneValidationResult Validate();
    }
}
