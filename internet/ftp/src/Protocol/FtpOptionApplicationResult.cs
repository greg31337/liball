namespace ftp;

internal class FtpOptionApplicationResult
{
    internal bool Handled;
    internal bool Accepted;
    internal bool ExtensionOwned;
    internal FtpOptionRequest Request;
    internal FtpReply? Reply;

    internal FtpOptionApplicationResult()
    {
        Handled = false;
        Accepted = false;
        ExtensionOwned = false;
        Request = new FtpOptionRequest();
        Reply = null;
    }
}
