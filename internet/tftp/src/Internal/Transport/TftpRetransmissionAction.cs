namespace tftp;

internal enum TftpRetransmissionAction
{
    Wait = 0,
    Retransmit = 1,
    RetryLimitReached = 2,
    TotalDurationReached = 3
}
