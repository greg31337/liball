namespace tftp;

internal class TftpSenderResult
{
    internal TftpSenderAction Action = TftpSenderAction.Wait;
    internal TftpPacket? Packet = null;
    internal long WaitMilliseconds = 0;
    internal bool IsFinalData = false;
    internal long BytesTransferred = 0;
    internal long EncodedBytesTransferred = 0;
    internal int RetransmissionCount = 0;
    internal int DuplicatePacketCount = 0;
}
