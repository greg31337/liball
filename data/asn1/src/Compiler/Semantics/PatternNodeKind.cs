namespace asn1;

internal enum PatternNodeKind
{
    Sequence = 0,
    Alternative = 1,
    Repeat = 2,
    Literal = 3,
    Any = 4,
    CharacterClass = 5,
    Range = 6,
    StartAnchor = 7,
    EndAnchor = 8
}
