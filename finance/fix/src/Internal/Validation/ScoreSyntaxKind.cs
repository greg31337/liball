namespace fix;

internal enum ScoreSyntaxKind
{
    Unknown = 0,
    Assignment = 1,
    Binary = 2,
    Unary = 3,
    SetMembership = 4,
    RangeMembership = 5,
    Exists = 6,
    Reference = 7,
    ReferencePart = 8,
    PredicateSelector = 9,
    IntegerLiteral = 10,
    DecimalLiteral = 11,
    CharacterLiteral = 12,
    StringLiteral = 13,
    DateLiteral = 14,
    TimeLiteral = 15,
    TimestampLiteral = 16,
    DurationLiteral = 17
}
