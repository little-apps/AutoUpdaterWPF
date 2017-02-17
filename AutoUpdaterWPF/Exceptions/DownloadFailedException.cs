using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
