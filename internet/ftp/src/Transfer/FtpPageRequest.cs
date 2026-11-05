namespace ftp;

internal class FtpPageRequest
{
    internal byte[]? PageIndex;
    internal int PageType;
    internal byte[]? OptionalHeaderFields;
    internal byte[]? Data;

    internal FtpPageRequest()
    {
        PageIndex = null;
        PageType = 1;
        OptionalHeaderFields = new byte[0];
        Data = new byte[0];
    }
}
