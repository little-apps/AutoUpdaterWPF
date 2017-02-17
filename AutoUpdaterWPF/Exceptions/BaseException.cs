using System;

namespace AutoUpdaterWPF.Exceptions
{
    public abstract class BaseException : Exception
    {
        protected BaseException(string message) : base(message)
        {
            AutoUpdater.Running = false;
        }

        protected BaseException(string message, Exception innerException) : base(message, innerException)
        {
            AutoUpdater.Running = false;
        }
    }
}
