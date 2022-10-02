using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Notifications.Kavita;

public class KavitaException : NzbDroneException
{
    public KavitaException(string message)
        : base(message)
    {
    }

    public KavitaException(string message, params object[] args)
        : base(message, args)
    {
    }
}
