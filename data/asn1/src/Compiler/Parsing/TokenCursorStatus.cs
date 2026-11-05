namespace asn1;

internal enum TokenCursorStatus
{
    NeedMoreData = 0,
    Token = 1,
    EndOfInput = 2,
    Mismatch = 3,
    LookaheadLimitExceeded = 4
}
