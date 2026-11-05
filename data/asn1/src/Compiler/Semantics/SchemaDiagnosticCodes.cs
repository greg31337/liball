namespace asn1;

internal static class SchemaDiagnosticCodes
{
    internal const int SelectionRequiresChoice = 1;
    internal const int SelectionAlternativeMissing = 2;
    internal const int ComponentsOfKindMismatch = 3;
    internal const int ComponentsOfEmpty = 4;
    internal const int IllegalImplicitTag = 5;
    internal const int DuplicateEffectiveTag = 6;
    internal const int OpenTagCollision = 7;
    internal const int MissingValueGovernor = 20;
    internal const int UnsupportedValueNotation = 21;
    internal const int IntegerValueMissing = 22;
    internal const int NonIntegerValueReference = 23;
    internal const int NamedNumberCycle = 24;
    internal const int NamedBitUnresolved = 25;
    internal const int NamedBitPositionUnsupported = 26;
    internal const int ObjectIdentifierComponentUnresolved = 27;
    internal const int ObjectIdentifierComponentKind = 28;
    internal const int IntegerExpressionInvalid = 29;
    internal const int RealValueInvalid = 30;
    internal const int UnicodeScalarInvalid = 31;
    internal const int RestrictedAlphabetViolation = 32;
    internal const int CharacterTupleInvalid = 33;
    internal const int BitStringPayloadInvalid = 34;
    internal const int ObjectIdentifierInvalid = 36;
    internal const int ObjectIdentifierIriInvalid = 37;
    internal const int TimeValueInvalid = 38;
    internal const int StructuredValueDuplicate = 39;
    internal const int StructuredValueUnknownComponent = 40;
    internal const int StructuredValueMissingComponent = 41;
    internal const int SequenceValueOrder = 42;
    internal const int ChoiceValueUnknownAlternative = 43;
    internal const int CollectionValueNames = 44;
    internal const int ExtensionRootInvalid = 45;
    internal const int ExtensionMarkerInvalid = 46;
    internal const int ComponentNameDuplicate = 47;
    internal const int EnumeratedItemConflict = 48;
    internal const int EnumeratedExtensionOrder = 49;
    internal const int ComponentOptionalityDefault = 50;
    internal const int VersionBracketConsistency = 51;
    internal const int ExtensionGroupVersion = 52;
}
