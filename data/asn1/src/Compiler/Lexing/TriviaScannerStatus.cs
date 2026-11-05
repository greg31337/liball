namespace asn1;

internal enum TriviaScannerStatus
{
    NeedMoreData = 0,
    Trivia = 1,
    NonTrivia = 2,
    EndOfInput = 3
}
