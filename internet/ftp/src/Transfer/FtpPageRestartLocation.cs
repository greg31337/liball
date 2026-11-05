namespace ftp;

internal class FtpPageRestartLocation
{
    internal byte[] PageIndex;
    internal long LogicalByteOffset;

    internal FtpPageRestartLocation(byte[] pageIndex, long logicalByteOffset)
    {
        PageIndex = pageIndex;
        LogicalByteOffset = logicalByteOffset;
    }
}
