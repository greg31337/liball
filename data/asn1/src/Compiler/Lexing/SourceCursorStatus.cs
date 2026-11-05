namespace asn1;

internal enum SourceCursorStatus
{
    NeedMoreData = 0,
    Value = 1,
    Invalid = 2,
    EndOfInput = 3
}
