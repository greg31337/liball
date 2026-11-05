namespace tftp;

internal class TftpRetransmissionResult
{
    internal TftpRetransmissionAction Action = TftpRetransmissionAction.Wait;
    internal long WaitMilliseconds = 0;
    internal long ElapsedMilliseconds = 0;
    internal int ConsecutiveRetransmissionCount = 0;
    internal int TotalRetransmissionCount = 0;
    internal long NextIntervalMilliseconds = 0;
}
