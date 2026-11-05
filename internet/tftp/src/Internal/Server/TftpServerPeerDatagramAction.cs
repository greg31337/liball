namespace tftp;

internal enum TftpServerPeerDatagramAction
{
    AcceptPacket = 0,
    SendError = 1,
    RejectUnknownTransferId = 2,
    IgnoreUnexpectedAddress = 3
}
