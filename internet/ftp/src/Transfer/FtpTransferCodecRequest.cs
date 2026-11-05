using System.IO;

namespace ftp;

internal class FtpTransferCodecRequest
{
    internal Stream? Source;
    internal Stream? Destination;
    internal FtpTransferOptions? Options;
    internal bool LeaveSourceOpen;
    internal bool LeaveDestinationOpen;
    internal bool HasLogicalByteCount;
    internal long LogicalByteCount;

    internal FtpTransferCodecRequest()
    {
        Source = null;
        Destination = null;
        Options = new FtpTransferOptions();
        LeaveSourceOpen = true;
        LeaveDestinationOpen = true;
        HasLogicalByteCount = false;
        LogicalByteCount = 0;
    }
}
