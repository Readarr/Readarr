namespace NzbDrone.Core.Notifications.Kavita;

public class KavitaAuthenticationException : KavitaException
{
    public KavitaAuthenticationException(string message)
        : base(message)
    {
    }

    public KavitaAuthenticationException(string message, params object[] args)
        : base(message, args)
    {
    }
}
