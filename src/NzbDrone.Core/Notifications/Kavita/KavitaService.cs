using System;
using FluentValidation.Results;
using NLog;

namespace NzbDrone.Core.Notifications.Kavita;

public interface IKavitaService
{
    void Notify(KavitaSettings settings, string message);
    ValidationFailure Test(KavitaSettings settings, string message);
}

public class KavitaService : IKavitaService
{
    private readonly IKavitaServiceProxy _proxy;
    private readonly Logger _logger;

    public KavitaService(IKavitaServiceProxy proxy,
        Logger logger)
    {
        _proxy = proxy;
        _logger = logger;
    }

    public void Notify(KavitaSettings settings, string folderPath)
    {
        _proxy.Notify(settings, folderPath);
    }

    private string GetToken(KavitaSettings settings)
    {
        return _proxy.GetToken(settings);
    }

    public ValidationFailure Test(KavitaSettings settings, string message)
    {
        try
        {
            _logger.Debug("Determining Authentication of Host: {0}", _proxy.GetBaseUrl(settings));
            var token = GetToken(settings);
            _logger.Debug("Token is: {0}", token);
        }
        catch (KavitaAuthenticationException ex)
        {
            _logger.Error(ex, "Unable to connect to Kavita Server");
            return new ValidationFailure("ApiKey", "Incorrect ApiKey");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unable to connect to Kavita Server");
            return new ValidationFailure("Host", "Unable to connect to Kavita Server");
        }

        return null;
    }
}
