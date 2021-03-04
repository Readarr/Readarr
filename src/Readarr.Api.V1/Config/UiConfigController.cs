using NzbDrone.Core.Configuration;
using Readarr.Http;

namespace Readarr.Api.V1.Config
{
    [V1ApiController("config/ui")]
    public class UiConfigController : ConfigController<UiConfigResource>
    {
        public UiConfigController(IConfigService configService)
            : base(configService)
        {
        }

        protected override UiConfigResource ToResource(IConfigService model)
        {
            return UiConfigResourceMapper.ToResource(model);
        }
    }
}
