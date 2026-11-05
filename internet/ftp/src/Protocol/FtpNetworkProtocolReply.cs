namespace ftp;

internal class FtpNetworkProtocolReply
{
    internal FtpReply Reply;
    internal int[] SupportedProtocols;

    internal FtpNetworkProtocolReply()
    {
        Reply = new FtpReply();
        SupportedProtocols = new int[0];
    }
}
