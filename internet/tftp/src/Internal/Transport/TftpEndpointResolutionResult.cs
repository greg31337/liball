using System.Net;
using System.Net.Sockets;

namespace tftp;

internal class TftpEndpointResolutionResult
{
    internal IPEndPoint RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
    internal IPEndPoint LocalEndPoint = new IPEndPoint(IPAddress.Any, 0);
    internal AddressFamily AddressFamily = AddressFamily.Unspecified;
}
