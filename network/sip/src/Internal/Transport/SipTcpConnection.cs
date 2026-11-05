namespace sip;

internal class SipTcpConnection
{
    internal System.Net.Sockets.Socket? Socket;
    internal SipReliableConnectionCore? Core;
    internal bool Closed;
}
