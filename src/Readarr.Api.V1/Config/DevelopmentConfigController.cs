using System.Linq;
using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Validation;
using NzbDrone.Http.REST.Attributes;
using Readarr.Http;
using Readarr.Http.REST;

namespace Prowlarr.Api.V1.Config
{
    [V1ApiController("config/development")]
    public class DevelopmentConfigController : RestController<DevelopmentConfigResource>
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IConfigService _configService;

        public DevelopmentConfigController(IConfigFileProvider configFileProvider,
                                IConfigService configService)
        {
            _configFileProvider = configFileProvider;
            _configService = configService;

            SharedValidator.RuleFor(c => c.MetadataSource).IsValidUrl().When(c => !c.MetadataSource.IsNullOrWhiteSpace());
        }

        protected override DevelopmentConfigResource GetResourceById(int id)
        {
            return GetDevelopmentConfig();
        }

        [HttpGet]
        public DevelopmentConfigResource GetDevelopmentConfig()
        {
            var resource = DevelopmentConfigResourceMapper.ToResource(_configFileProvider, _configService);
            resource.Id = 1;

            return resource;
        }

        [RestPutById]
        public ActionResult<DevelopmentConfigResource> SaveDevelopmentConfig(DevelopmentConfigResource resource)
        {
            var dictionary = resource.GetType()
                                     .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                     .ToDictionary(prop => prop.Name, prop => prop.GetValue(resource, null));

            _configFileProvider.SaveConfigDictionary(dictionary);
            _configService.SaveConfigDictionary(dictionary);

            return Accepted(resource.Id);
        }
    }
}
