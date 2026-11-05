namespace iso20022;

/// <summary>Defines caller-adjustable bounds for parsing and validation resources.</summary>
public class ResourceLimits
{
    /// <summary>The maximum XML element depth.</summary>
    public int MaxDepth = Iso20022Limits.MaxDepth;
    /// <summary>The maximum attributes on one element.</summary>
    public int MaxAttributesPerElement = Iso20022Limits.MaxAttributesPerElement;
    /// <summary>The maximum characters in one text value.</summary>
    public int MaxTextLength = Iso20022Limits.MaxTextLength;
    /// <summary>The maximum element count.</summary>
    public int MaxElementCount = Iso20022Limits.MaxElementCount;
    /// <summary>The maximum items in one collection.</summary>
    public int MaxCollectionCount = Iso20022Limits.MaxCollectionCount;
    /// <summary>The maximum input size in bytes.</summary>
    public int MaxTotalBytes = Iso20022Limits.MaxTotalBytes;
    /// <summary>The maximum schema count.</summary>
    public int MaxSchemaCount = Iso20022Limits.MaxSchemaCount;
    /// <summary>The maximum diagnostic count.</summary>
    public int MaxDiagnosticCount = Iso20022Limits.MaxDiagnosticCount;
}
