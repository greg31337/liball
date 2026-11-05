namespace ftp;

internal class FtpResourceLimitException : System.Exception
{
    internal FtpResourceLimitException(string message)
        : base(message)
    {
    }
}
