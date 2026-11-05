using System.Net.Sockets;

namespace ftp;

internal class FtpPassiveConnection
{
    internal Socket? Connection;
    internal FtpEndpoint Endpoint;
    internal bool Extended;
    internal bool UsedControlPeerAddress;

    internal FtpPassiveConnection()
    {
        Connection = null;
        Endpoint = new FtpEndpoint();
        Extended = false;
        UsedControlPeerAddress = false;
    }
}
