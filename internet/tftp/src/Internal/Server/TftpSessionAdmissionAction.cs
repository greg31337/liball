namespace tftp;

internal enum TftpSessionAdmissionAction
{
    Accepted = 0,
    Duplicate = 1,
    SessionLimitReached = 2,
    ServerStopping = 3,
    InvalidRequest = 4
}
