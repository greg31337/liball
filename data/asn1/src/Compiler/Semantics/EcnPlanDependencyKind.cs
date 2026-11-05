namespace asn1;

internal enum EcnPlanDependencyKind
{
    Ordering = 0,
    FieldValue = 1,
    DeterminantValue = 2,
    BitPosition = 3,
    BytePosition = 4,
    EncodingSpace = 5,
    EncodingObject = 6,
    EncodingObjectSet = 7,
    TransformResult = 8,
    ConditionValue = 9,
    ReplacementValue = 10
}
