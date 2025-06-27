using System;
using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.ImportLists.NYTimes
{
    public class NYTimesException : NzbDroneException
    {
        public NYTimesException(string message)
            : base(message)
        {
        }

        public NYTimesException(string message, params object[] args)
            : base(message, args)
        {
        }

        public NYTimesException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class NYTimesAuthorizationException : NYTimesException
    {
        public NYTimesAuthorizationException(string message)
            : base(message)
        {
        }
    }
}
