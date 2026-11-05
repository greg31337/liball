using System.Net.Sockets;

namespace ftp;

internal class FtpPassiveAcceptState
{
    internal Socket? Connection;

    internal FtpPassiveAcceptState()
    {
        Connection = null;
    }
}
