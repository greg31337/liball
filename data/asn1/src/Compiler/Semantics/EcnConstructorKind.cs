namespace asn1;

internal enum EcnConstructorKind
{
    Unknown = 0,
    Concatenation = 1,
    Alternatives = 2,
    Repetition = 3,
    Optionality = 4,
    Tag = 5,
    ConditionalInteger = 6,
    ConditionalRepetition = 7
}
