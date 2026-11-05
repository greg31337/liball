namespace ftp;

internal class FtpThirdPartyTransferState
{
    internal bool TargetPassiveSetupPending;
    internal bool TargetPassiveConfigured;
    internal bool SourceActiveSetupPending;
    internal bool SourceActiveConfigured;
    internal bool TransferStarted;
    internal bool SourceTransferPending;
    internal bool TargetTransferPending;
    internal bool SourceConnectionClosed;
    internal bool TargetConnectionClosed;
    internal bool Complete;
    internal bool Succeeded;
    internal FtpEndpoint? PassiveEndpoint;
    internal FtpCommand? TargetPassiveCommand;
    internal FtpCommand? SourceActiveCommand;
    internal FtpReply? TargetPassiveReply;
    internal FtpReply? SourceActiveReply;
    internal FtpCommandResult[] SourceCommandResults;
    internal FtpCommandResult[] TargetCommandResults;
    internal FtpCommandResult? SourceTransferResult;
    internal FtpCommandResult? TargetTransferResult;

    internal FtpThirdPartyTransferState()
    {
        TargetPassiveSetupPending = false;
        TargetPassiveConfigured = false;
        SourceActiveSetupPending = false;
        SourceActiveConfigured = false;
        TransferStarted = false;
        SourceTransferPending = false;
        TargetTransferPending = false;
        SourceConnectionClosed = false;
        TargetConnectionClosed = false;
        Complete = false;
        Succeeded = false;
        PassiveEndpoint = null;
        TargetPassiveCommand = null;
        SourceActiveCommand = null;
        TargetPassiveReply = null;
        SourceActiveReply = null;
        SourceCommandResults = new FtpCommandResult[0];
        TargetCommandResults = new FtpCommandResult[0];
        SourceTransferResult = null;
        TargetTransferResult = null;
    }
}
