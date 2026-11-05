namespace tftp;

internal enum TftpBlockClassification
{
    Expected = 0,
    Previous = 1,
    HandshakeAcknowledgement = 2,
    Unexpected = 3
}
