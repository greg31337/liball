namespace ftp;

internal class FtpServerFileTransferState
{
    internal FtpFileSystemRequest Request;
    internal FtpFileOpenResult? OpenResult;
    internal FtpClearDataStream? Transfer;
    internal FtpProtectedDataStream? ProtectedTransfer;
    internal FtpReply? FinalReply;
    internal bool PreliminarySent;
    internal bool TransferLimitAcquired;
    internal string? UniquePath;
    internal bool HasMaximumBytes;
    internal long MaximumBytes;
    internal bool HasStorageMaximumBytes;
    internal long StorageMaximumBytes;
    internal bool HasBytesPerSecond;
    internal long BytesPerSecond;
    internal int ResourceFailureCode;

    internal FtpServerFileTransferState()
    {
        Request = new FtpFileSystemRequest();
        OpenResult = null;
        Transfer = null;
        ProtectedTransfer = null;
        FinalReply = null;
        PreliminarySent = false;
        TransferLimitAcquired = false;
        UniquePath = null;
        HasMaximumBytes = false;
        MaximumBytes = 0;
        HasStorageMaximumBytes = false;
        StorageMaximumBytes = 0;
        HasBytesPerSecond = false;
        BytesPerSecond = 0;
        ResourceFailureCode = 0;
    }
}
