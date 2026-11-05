namespace tftp;

internal enum TftpSenderAction
{
    SendData = 0,
    RetransmitData = 1,
    Wait = 2,
    IgnoreDuplicateAcknowledgement = 3,
    Completed = 4
}
