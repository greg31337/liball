namespace ftp;

internal class FtpSequenceValidationResult
{
    internal bool Allowed;
    internal int ReplyCode;
    internal string Message;
    internal FtpCommandDefinition Definition;

    internal FtpSequenceValidationResult()
    {
        Allowed = false;
        ReplyCode = FtpReplyCodes.BadCommandSequence;
        Message = "The FTP command is not valid in the current sequence.";
        Definition = new FtpCommandDefinition();
    }
}
