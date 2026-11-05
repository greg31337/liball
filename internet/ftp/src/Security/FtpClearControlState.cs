namespace ftp;

internal class FtpClearControlState
{
    internal bool CommandPending;
    internal bool ReplyWritePending;
    internal bool TlsShutdownPending;
    internal bool TlsShutdownComplete;
    internal bool ClearControlRestored;
    internal bool Rejected;
    internal bool Failed;
    internal bool ConnectionMustClose;
    internal bool AuditRecorded;
    internal FtpCommand? Command;
    internal FtpReply? Reply;
    internal FtpEvent? AuditEvent;

    internal FtpClearControlState()
    {
        CommandPending = false;
        ReplyWritePending = false;
        TlsShutdownPending = false;
        TlsShutdownComplete = false;
        ClearControlRestored = false;
        Rejected = false;
        Failed = false;
        ConnectionMustClose = false;
        AuditRecorded = false;
        Command = null;
        Reply = null;
        AuditEvent = null;
    }
}
