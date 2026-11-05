namespace ftp;

internal class FtpPassiveReply
{
    internal FtpReply Reply;
    internal FtpEndpoint Endpoint;
    internal bool Extended;

    internal FtpPassiveReply()
    {
        Reply = new FtpReply();
        Endpoint = new FtpEndpoint();
        Extended = false;
    }
}
