namespace tftp;

internal class TftpSessionAdmissionResult
{
    internal TftpSessionAdmissionAction Action = TftpSessionAdmissionAction.InvalidRequest;
    internal string RequestIdentity = "";
}
