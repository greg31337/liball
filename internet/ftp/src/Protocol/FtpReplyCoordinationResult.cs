namespace ftp;

internal class FtpReplyCoordinationResult
{
    internal bool DataCommandPending;
    internal bool AbortCommandPending;
    internal bool ConnectionClosed;
    internal FtpReply? AsynchronousReply;
    internal FtpCommandResult? CompletedDataCommand;
    internal FtpCommandResult? CompletedAbortCommand;

    internal FtpReplyCoordinationResult()
    {
        DataCommandPending = false;
        AbortCommandPending = false;
        ConnectionClosed = false;
        AsynchronousReply = null;
        CompletedDataCommand = null;
        CompletedAbortCommand = null;
    }
}
