namespace ftp;

internal class FtpPendingReply
{
    internal FtpReply Reply;
    internal string Operation;
    internal string Message;

    internal FtpPendingReply()
    {
        Reply = new FtpReply();
        Operation = "";
        Message = "";
    }
}
