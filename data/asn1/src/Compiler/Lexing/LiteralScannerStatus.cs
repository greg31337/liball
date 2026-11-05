namespace asn1;

internal enum LiteralScannerStatus
{
    NeedMoreData = 0,
    Token = 1,
    Trivia = 2,
    NonLiteral = 3,
    EndOfInput = 4
}
