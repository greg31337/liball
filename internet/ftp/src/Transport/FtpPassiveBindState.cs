using System.Net.Sockets;

namespace ftp;

internal class FtpPassiveBindState
{
    internal Socket? Listener;

    internal FtpPassiveBindState()
    {
        Listener = null;
    }
}
