using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdaterWPF.Exceptions
{
    public abstract class BaseException : Exception
    {
        protected BaseException(string message) : base(message)
        {
            
        }

        protected BaseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
