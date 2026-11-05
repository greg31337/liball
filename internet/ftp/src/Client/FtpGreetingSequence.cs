namespace ftp;

internal class FtpGreetingSequence
{
    internal FtpReply InitialReply;
    internal FtpReply ReadyReply;
    internal bool Delayed;

    internal FtpGreetingSequence()
    {
        InitialReply = new FtpReply();
        ReadyReply = new FtpReply();
        Delayed = false;
    }
}
