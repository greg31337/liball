namespace asn1;

internal enum TriviaScannerState
{
    Idle = 0,
    Whitespace = 1,
    HyphenComment = 2,
    BlockComment = 3
}
