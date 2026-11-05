using System.Net;

namespace tftp;

internal class UdpDatagramReceiveResult
{
    internal bool HasDatagram = false;
    internal int Length = 0;
    internal IPEndPoint RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
    internal IPAddress DestinationAddress = IPAddress.Any;
    internal bool ExceededMaximumLength = false;
}
