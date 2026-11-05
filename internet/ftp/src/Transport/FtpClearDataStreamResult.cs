namespace ftp;

internal class FtpClearDataStreamResult
{
    internal FtpReply PreliminaryReply;
    internal FtpReply? FinalReply;
    internal FtpTransferPumpResult? PumpResult;
    internal bool Sending;
    internal bool DataConnectionClosed;
    internal bool Succeeded;

    internal FtpClearDataStreamResult()
    {
        PreliminaryReply = new FtpReply();
        FinalReply = null;
        PumpResult = null;
        Sending = false;
        DataConnectionClosed = false;
        Succeeded = false;
    }
}
