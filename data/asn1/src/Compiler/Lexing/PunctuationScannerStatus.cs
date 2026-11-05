namespace asn1;

internal enum PunctuationScannerStatus
{
    NeedMoreData = 0,
    Token = 1,
    Trivia = 2,
    NonPunctuation = 3,
    EndOfInput = 4
}
