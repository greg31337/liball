namespace sip;

internal class SipWebSocketConnection
{
    internal System.Net.Sockets.Socket? Socket;
    internal System.IO.Stream? Stream;
    internal System.Net.Security.SslStream? SecureStream;
    internal System.Net.WebSockets.WebSocket? WebSocket;
    internal System.Threading.Tasks.Task<bool>? HandshakeTask;
    internal System.Threading.Tasks.Task<System.Net.WebSockets.WebSocketReceiveResult>?
        ReadTask;
    internal byte[] ReadBuffer = new byte[0];
    internal System.Collections.Generic.List<byte> MessageBuffer =
        new System.Collections.Generic.List<byte>();
    internal System.Net.WebSockets.WebSocketMessageType MessageType;
    internal bool MessageComplete;
    internal System.Threading.Tasks.Task? WriteTask;
    internal int WriteCount;
    internal SipReliableConnectionCore? Core;
    internal bool ClientSide;
    internal bool Closed;
}
