using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Calibre
{
    public class Calibre : NotificationBase<CalibreSettings>
    {
        private readonly ICalibreProxy _proxy;
        private readonly Logger _logger;

        public Calibre(ICalibreProxy proxy,
                       Logger logger)
        {
            _proxy = proxy;
            _logger = logger;
        }

        public override string Link => "https://github.com/Readarr/Readarr/wiki/Calibre";

        public override string Name => "Calibre";

        public override void OnReleaseImport(AlbumDownloadMessage message)
        {
            _logger.Trace($"Calibre import for {message.Album.Title}");
            foreach (var file in message.TrackFiles)
            {
                _logger.Trace($"Importing to calibre: {file.Path}");
                var import = _proxy.AddFile(file, Settings);

                if (Settings.OutputFormat != (int)CalibreFormat.None)
                {
                    _logger.Trace($"Getting book data for {import.Id}");
                    var options = _proxy.GetBookData(import.Id, Settings);

                    options.Conversion_options.Input_fmt = options.Input_formats.First();
                    options.Conversion_options.Output_fmt = ((CalibreFormat)Settings.OutputFormat).ToString();

                    if (Settings.OutputProfile != (int)CalibreProfile.Default)
                    {
                        options.Conversion_options.Options.Output_profile = ((CalibreProfile)Settings.OutputProfile).ToString();
                    }

                    _logger.Trace($"Starting conversion to {Settings.OutputFormat}");
                    _proxy.ConvertBook(import.Id, options.Conversion_options, Settings);
                }
            }
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(SendCalibreTest());

            return new ValidationResult(failures);
        }

        private ValidationFailure SendCalibreTest()
        {
            try
            {
                _proxy.GetListing(Settings);
            }
            catch (CalibreException ex)
            {
                return new NzbDroneValidationFailure("Url", ex.Message);
            }

            return null;
        }
    }
}
