using System.IO;

namespace ftp;

internal class FtpServerListingTransferState
{
    internal MemoryStream Stream;
    internal FtpClearDataStream? ClearTransfer;
    internal FtpProtectedDataStream? ProtectedTransfer;
    internal FtpReply? FinalReply;
    internal bool TransferLimitAcquired;

    internal FtpServerListingTransferState(MemoryStream stream)
    {
        Stream = stream;
        ClearTransfer = null;
        ProtectedTransfer = null;
        FinalReply = null;
        TransferLimitAcquired = true;
    }
}
