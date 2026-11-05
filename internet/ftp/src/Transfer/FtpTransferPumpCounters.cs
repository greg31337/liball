namespace ftp;

internal class FtpTransferPumpCounters
{
    internal long SourceBytes;
    internal long DestinationBytes;
    internal long LogicalBytes;
    internal long WireBytes;

    internal FtpTransferPumpCounters(long logicalBytes, long wireBytes)
    {
        SourceBytes = 0;
        DestinationBytes = 0;
        LogicalBytes = logicalBytes;
        WireBytes = wireBytes;
    }
}
