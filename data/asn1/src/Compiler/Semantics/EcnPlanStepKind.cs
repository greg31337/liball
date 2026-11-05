namespace asn1;

internal enum EcnPlanStepKind
{
    Unknown = 0,
    Primitive = 1,
    Sequence = 2,
    Choice = 3,
    Repetition = 4,
    Optional = 5,
    Tag = 6,
    Field = 7,
    Property = 8,
    Length = 9,
    Alignment = 10,
    Padding = 11,
    Condition = 12,
    DeterminantRead = 13,
    DeterminantWrite = 14,
    Mapping = 15,
    Replacement = 16,
    Transformation = 17,
    OpenType = 18,
    StandardEncoding = 19
}
