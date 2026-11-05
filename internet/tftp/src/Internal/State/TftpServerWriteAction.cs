namespace tftp;

internal enum TftpServerWriteAction
{
    SendOptionAcknowledgement = 0,
    SendAcknowledgement = 1,
    ReacknowledgeData = 2,
    RetransmitPacket = 3,
    Wait = 4,
    SendError = 5,
    Completed = 6
}
