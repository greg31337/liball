namespace ftp;

internal class FtpTransferPumpChunk
{
    internal byte[]? Data;
    internal int Count;
    internal long LogicalBytes;
    internal long WireBytes;

    internal FtpTransferPumpChunk()
    {
        Data = null;
        Count = 0;
        LogicalBytes = 0;
        WireBytes = 0;
    }
}
