namespace tftp;

internal class TftpServerSessionStartResult
{
    internal TftpSessionAdmissionAction Action =
        TftpSessionAdmissionAction.InvalidRequest;
    internal TftpServerSession? Session = null;
    internal bool ResponseResent = false;
}
