using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdaterWPF.Exceptions
{
    public class AlreadyRunningException : BaseException
    {
        public AlreadyRunningException() : base("Another update check is already running")
        {
        }
    }
}
