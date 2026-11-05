namespace tftp;

internal enum TftpServerReadAction
{
    SendOptionAcknowledgement = 0,
    SendData = 1,
    RetransmitPacket = 2,
    Wait = 3,
    IgnoreDuplicateAcknowledgement = 4,
    SendError = 5,
    Completed = 6
}
