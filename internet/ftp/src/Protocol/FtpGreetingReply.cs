namespace ftp;

internal class FtpGreetingReply
{
    internal FtpReply Reply;
    internal bool Ready;
    internal bool Delayed;
    internal string Message;

    internal FtpGreetingReply()
    {
        Reply = new FtpReply();
        Ready = false;
        Delayed = false;
        Message = "";
    }
}
