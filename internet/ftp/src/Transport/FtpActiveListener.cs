using System.Net.Sockets;

namespace ftp;

internal class FtpActiveListener
{
    internal Socket? Listener;
    internal FtpEndpoint BindEndpoint;
    internal FtpEndpoint AdvertisedEndpoint;
    internal bool Open;

    internal FtpActiveListener()
    {
        Listener = null;
        BindEndpoint = new FtpEndpoint();
        AdvertisedEndpoint = new FtpEndpoint();
        Open = false;
    }
}
