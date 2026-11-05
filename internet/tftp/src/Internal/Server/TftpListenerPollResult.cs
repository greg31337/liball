namespace tftp;

internal class TftpListenerPollResult
{
    internal bool HasDatagram = false;
    internal UdpDatagramSocket? Listener = null;
    internal UdpDatagramReceiveResult? Datagram = null;
    internal byte[] Buffer = [];
}
