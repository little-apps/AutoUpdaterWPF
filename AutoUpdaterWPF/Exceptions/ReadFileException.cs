using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
