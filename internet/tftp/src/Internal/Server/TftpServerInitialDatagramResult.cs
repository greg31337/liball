namespace tftp;

internal class TftpServerInitialDatagramResult
{
    internal TftpServerInitialDatagramAction Action =
        TftpServerInitialDatagramAction.Ignore;
    internal TftpPacket RequestPacket = new TftpPacket();
    internal TftpPacket? ErrorPacket = null;
    internal string FailureMessage = "";
}
