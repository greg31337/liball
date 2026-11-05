namespace tftp;

internal enum TftpPeerValidation
{
    Candidate = 0,
    Expected = 1,
    UnknownTransferId = 2,
    UnexpectedAddress = 3
}
