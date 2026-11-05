namespace ftp;

internal class FtpFaultIsolationResult
{
    internal bool Succeeded;
    internal bool Canceled;
    internal object? Value;
    internal System.Exception? Failure;

    internal FtpFaultIsolationResult()
    {
        Succeeded = false;
        Canceled = false;
        Value = null;
        Failure = null;
    }
}
