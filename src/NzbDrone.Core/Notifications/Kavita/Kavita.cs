using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Notifications.Kavita;

public class Kavita : NotificationBase<KavitaSettings>
{
    private readonly IKavitaService _kavitaService;
    private readonly Logger _logger;

    public Kavita(IKavitaService kavitaService, Logger logger)
    {
        _kavitaService = kavitaService;
        _logger = logger;
    }

    public override string Link => "https://www.kavitareader.com/";

    public override void OnReleaseImport(BookDownloadMessage message)
    {
        var allPaths = message.BookFiles.Select(v => v.Path).Distinct();
        var path = Directory.GetParent(allPaths.First())?.FullName;
        Notify(Settings, BOOK_DOWNLOADED_TITLE_BRANDED, path);
    }

    public override void OnBookDelete(BookDeleteMessage deleteMessage)
    {
        var allPaths = deleteMessage.Book.BookFiles.Value.Select(v => v.Path).Distinct();
        var path = Directory.GetParent(allPaths.First())?.FullName;
        Notify(Settings, BOOK_FILE_DELETED_TITLE_BRANDED, path);
    }

    public override void OnBookFileDelete(BookFileDeleteMessage message)
    {
        Notify(Settings, BOOK_FILE_DELETED_TITLE_BRANDED, Directory.GetParent(message.BookFile.Path)?.FullName);
    }

    public override void OnBookRetag(BookRetagMessage message)
    {
        Notify(Settings, BOOK_RETAGGED_TITLE_BRANDED, Directory.GetParent(message.BookFile.Path)?.FullName);
    }

    public override string Name => "Kavita";

    public override ValidationResult Test()
    {
        var failures = new List<ValidationFailure>();

        failures.AddIfNotNull(_kavitaService.Test(Settings, "Success! Kavita has been successfully configured!"));

        return new ValidationResult(failures);
    }

    private void Notify(KavitaSettings settings, string header, string message)
    {
        try
        {
            if (Settings.Notify)
            {
                _kavitaService.Notify(Settings, $"{header} - {message}");
            }
        }
        catch (SocketException ex)
        {
            var logMessage = $"Unable to connect to Subsonic Host: {Settings.Host}:{Settings.Port}";
            _logger.Debug(ex, logMessage);
        }
    }
}
