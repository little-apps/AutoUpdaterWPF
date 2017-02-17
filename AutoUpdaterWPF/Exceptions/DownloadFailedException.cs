using System;

namespace AutoUpdaterWPF.Exceptions
{
    public class DownloadFailedException : BaseException
    {
        public readonly string DownloadUrl;

        public DownloadFailedException(string downloadUrl, Exception innerException)
            : base($"Unable to download update: {innerException.Message}", innerException)
        {
            DownloadUrl = downloadUrl;
        }
    }
}
