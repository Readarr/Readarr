using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Localization;
using Readarr.Http;

namespace Readarr.Api.V1.Localization
{
    [V1ApiController]
    public class LocalizationController : Controller
    {
        private readonly ILocalizationService _localizationService;
        private readonly JsonSerializerOptions _serializerSettings;

        public LocalizationController(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
            _serializerSettings = STJson.GetSerializerSettings();
            _serializerSettings.DictionaryKeyPolicy = null;
            _serializerSettings.PropertyNamingPolicy = null;
        }

        [HttpGet]
        public string GetLocalizationDictionary()
        {
            return JsonSerializer.Serialize(_localizationService.GetLocalizationDictionary().ToResource(), _serializerSettings);
        }
    }
}
