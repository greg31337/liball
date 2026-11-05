namespace asn1;

internal enum LiteralScannerState
{
    Idle = 0,
    Number = 1,
    RadixString = 2,
    CharacterString = 3,
    Tuple = 4
}
