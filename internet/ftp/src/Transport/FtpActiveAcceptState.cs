using System.Net.Sockets;

namespace ftp;

internal class FtpActiveAcceptState
{
    internal Socket? Connection;

    internal FtpActiveAcceptState()
    {
        Connection = null;
    }
}
