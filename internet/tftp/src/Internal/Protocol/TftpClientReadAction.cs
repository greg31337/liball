namespace tftp;

internal enum TftpClientReadAction
{
    SendRequest = 0,
    Wait = 1,
    IgnoreUnexpectedPeer = 2,
    RejectUnknownTransferId = 3,
    SendAcknowledgement = 4,
    ReacknowledgeData = 5,
    RetransmitPacket = 6,
    SendError = 7,
    Completed = 8
}
