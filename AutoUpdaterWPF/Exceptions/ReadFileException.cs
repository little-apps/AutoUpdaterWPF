using System;

namespace AutoUpdaterWPF.Exceptions
{
    public class ReadFileException : BaseException
    {
        public ReadFileException(Exception innerException)
            : base($"The following error occurred trying to read update file: {innerException.Message}", innerException)
        {
        }
    }
}
