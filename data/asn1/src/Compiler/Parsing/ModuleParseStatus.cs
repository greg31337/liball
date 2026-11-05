namespace asn1;

internal enum ModuleParseStatus
{
    Parsed = 0,
    NeedMoreData = 1,
    EndOfInput = 2,
    Invalid = 3
}
