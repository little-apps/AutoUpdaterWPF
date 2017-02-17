namespace AutoUpdaterWPF.Exceptions
{
    public class AlreadyRunningException : BaseException
    {
        public AlreadyRunningException() : base("Another update check is already running")
        {
        }
    }
}
