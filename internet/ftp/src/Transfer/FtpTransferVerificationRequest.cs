namespace ftp;

internal class FtpTransferVerificationRequest
{
    internal FtpVerificationMode Mode;
    internal bool HasExpectedSize;
    internal long ExpectedSize;
    internal bool HasActualSize;
    internal long ActualSize;
    internal FtpTimestamp? ExpectedModifiedTime;
    internal FtpTimestamp? ActualModifiedTime;
    internal string? ExpectedChecksumAlgorithm;
    internal string? ExpectedChecksum;
    internal string? ActualChecksumAlgorithm;
    internal string? ActualChecksum;

    internal FtpTransferVerificationRequest()
    {
        Mode = FtpVerificationMode.None;
        HasExpectedSize = false;
        ExpectedSize = 0;
        HasActualSize = false;
        ActualSize = 0;
        ExpectedModifiedTime = null;
        ActualModifiedTime = null;
        ExpectedChecksumAlgorithm = null;
        ExpectedChecksum = null;
        ActualChecksumAlgorithm = null;
        ActualChecksum = null;
    }
}
