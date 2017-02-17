using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdaterWPF.Exceptions
{
    class SaveFileException : BaseException
    {
        public SaveFileException(Exception innerException)
            : base(
                $"The following error occurred trying to save update settings: {innerException.Message}", innerException
            )
        {
        }
    }
}
