namespace tftp;

internal enum TftpClientWriteAction
{
    SendRequest = 0,
    Wait = 1,
    IgnoreUnexpectedPeer = 2,
    RejectUnknownTransferId = 3,
    SendData = 4,
    RetransmitData = 5,
    IgnoreDuplicateAcknowledgement = 6,
    SendError = 7,
    Completed = 8
}
