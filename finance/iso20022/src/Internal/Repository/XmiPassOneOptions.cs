namespace iso20022;

internal class XmiPassOneOptions
{
    internal int MaximumCharacters = Iso20022Limits.MaxArtifactBytes;
    internal int MaximumDepth = Iso20022Limits.MaxDepth;
    internal int MaximumAttributesPerElement = Iso20022Limits.MaxAttributesPerElement;
    internal int MaximumElementCount = Iso20022Limits.MaxElementCount;
    internal int MaximumIdentifierCount = Iso20022Limits.MaxElementCount;
    internal int MaximumDuplicateCount = Iso20022Limits.MaxDiagnosticCount;
}
