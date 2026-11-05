namespace ftp;

internal class FtpPageFrame
{
    internal int HeaderLogicalByteCount;
    internal int DataLogicalByteCount;
    internal byte[] PageIndex;
    internal int PageType;
    internal byte[] OptionalHeaderFields;
    internal byte[] Data;
    internal byte[] SerializedLogicalBytes;
    internal FtpPageRestartLocation? RestartLocation;
    internal bool LastPage;

    internal FtpPageFrame()
    {
        HeaderLogicalByteCount = 0;
        DataLogicalByteCount = 0;
        PageIndex = new byte[0];
        PageType = 0;
        OptionalHeaderFields = new byte[0];
        Data = new byte[0];
        SerializedLogicalBytes = new byte[0];
        RestartLocation = null;
        LastPage = false;
    }
}
