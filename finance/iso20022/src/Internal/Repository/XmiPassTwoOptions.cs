namespace iso20022;

internal class XmiPassTwoOptions
{
    internal XmiPassOneOptions ReaderLimits = new XmiPassOneOptions();
    internal int MaximumAttributeCount = 5000000;
    internal int MaximumRelationshipCount = 10000000;
    internal int MaximumDiagnosticCount = Iso20022Limits.MaxDiagnosticCount;
    internal int MaximumAttributeValueLength = Iso20022Limits.MaxTextLength;
}
