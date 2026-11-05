namespace sip;

internal class SipTlsConnection
{
    internal System.Net.Sockets.Socket? Socket;
    internal System.Net.Security.SslStream? Stream;
    internal System.Threading.Tasks.Task? HandshakeTask;
    internal System.Threading.Tasks.Task<int>? ReadTask;
    internal byte[] ReadBuffer = new byte[0];
    internal System.Threading.Tasks.Task? WriteTask;
    internal int WriteCount;
    internal SipReliableConnectionCore? Core;
    internal bool ClientSide;
    internal bool Closed;
}
