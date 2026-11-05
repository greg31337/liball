namespace tftp;

internal class TftpServerPeerDatagramResult
{
    internal TftpServerPeerDatagramAction Action =
        TftpServerPeerDatagramAction.IgnoreUnexpectedAddress;
    internal TftpPacket? Packet = null;
    internal TftpPacket? ErrorPacket = null;
}
