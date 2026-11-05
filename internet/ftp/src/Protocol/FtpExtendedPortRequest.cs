namespace ftp;

internal class FtpExtendedPortRequest
{
    internal int Protocol;
    internal bool Supported;
    internal FtpEndpoint Endpoint;

    internal FtpExtendedPortRequest()
    {
        Protocol = 0;
        Supported = false;
        Endpoint = new FtpEndpoint();
    }
}
