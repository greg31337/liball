namespace asn1;

internal enum EcnDeterminationMode
{
    Unknown = 0,
    FieldToBeSet = 1,
    FieldToBeUsed = 2,
    FlagToBeSet = 3,
    FlagToBeUsed = 4,
    Container = 5,
    Pattern = 6,
    Handle = 7,
    Pointer = 8,
    NotNeeded = 9
}
