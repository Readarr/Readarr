using NzbDrone.Core.Configuration;
using Readarr.Http.REST;

namespace Prowlarr.Api.V1.Config
{
    public class DevelopmentConfigResource : RestResource
    {
        public string ConsoleLogLevel { get; set; }
        public bool LogSql { get; set; }
        public int LogRotate { get; set; }
        public bool FilterSentryEvents { get; set; }
    }

    public static class DevelopmentConfigResourceMapper
    {
        public static DevelopmentConfigResource ToResource(this IConfigFileProvider model, IConfigService configService)
        {
            return new DevelopmentConfigResource
            {
                ConsoleLogLevel = model.ConsoleLogLevel,
                LogSql = model.LogSql,
                LogRotate = model.LogRotate,
                FilterSentryEvents = model.FilterSentryEvents
            };
        }
    }
}
