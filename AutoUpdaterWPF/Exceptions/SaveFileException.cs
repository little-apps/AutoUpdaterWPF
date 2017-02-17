using System;

namespace AutoUpdaterWPF.Exceptions
{
    public class SaveFileException : BaseException
    {
        public SaveFileException(Exception innerException)
            : base(
                $"The following error occurred trying to save update settings: {innerException.Message}", innerException
            )
        {
        }
    }
}
