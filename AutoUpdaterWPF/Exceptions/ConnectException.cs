using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdaterWPF.Exceptions
{
    class ConnectException : BaseException
    {
        public ConnectException(Exception innerException)
            : base($"The following error occurred trying to connect to update server: {innerException.Message}", innerException)
        {
            
        }

        public ConnectException(string message, Exception innerException) : base(message, innerException)
        {
            
        }
    }
}
