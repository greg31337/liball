namespace asn1;

internal enum IdentifierScannerStatus
{
    NeedMoreData = 0,
    Token = 1,
    Trivia = 2,
    NonIdentifier = 3,
    EndOfInput = 4
}
