namespace ftp;

internal class FtpTransferPumpResult
{
    internal long SourceBytes;
    internal long DestinationBytes;
    internal FtpTransferProgress Progress;

    internal FtpTransferPumpResult()
    {
        SourceBytes = 0;
        DestinationBytes = 0;
        Progress = new FtpTransferProgress();
    }
}
