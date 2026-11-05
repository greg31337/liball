namespace tftp;

internal enum TftpReceiverAction
{
    Wait = 0,
    SendAcknowledgement = 1,
    ReacknowledgeDuplicateData = 2,
    RetransmitPacket = 3,
    Completed = 4
}
