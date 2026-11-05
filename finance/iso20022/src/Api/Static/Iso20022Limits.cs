namespace iso20022;

/// <summary>Contains conservative default resource limits for message processing.</summary>
public static class Iso20022Limits
{
    /// <summary>The default maximum XML element depth.</summary>
    public const int MaxDepth = 128;
    /// <summary>The default maximum attributes on one element.</summary>
    public const int MaxAttributesPerElement = 256;
    /// <summary>The default maximum characters in one text value.</summary>
    public const int MaxTextLength = 1048576;
    /// <summary>The default maximum element count.</summary>
    public const int MaxElementCount = 1000000;
    /// <summary>The default maximum items in one collection.</summary>
    public const int MaxCollectionCount = 100000;
    /// <summary>The default maximum input size in bytes.</summary>
    public const int MaxTotalBytes = 67108864;
    /// <summary>The default maximum schema count.</summary>
    public const int MaxSchemaCount = 1024;
    /// <summary>The default maximum diagnostic count.</summary>
    public const int MaxDiagnosticCount = 1000;
    /// <summary>The default maximum repository artifact size in bytes.</summary>
    public const int MaxArtifactBytes = 536870912;
    /// <summary>The default maximum number of entries in one repository archive.</summary>
    public const int MaxArchiveEntryCount = 256;
    /// <summary>The default maximum expanded size of one repository archive entry.</summary>
    public const int MaxArchiveEntryBytes = 268435456;
    /// <summary>The default maximum combined expanded size of repository archive entries.</summary>
    public const int MaxArchiveExpandedBytes = 536870912;
    /// <summary>The default maximum ratio of expanded to compressed archive bytes.</summary>
    public const int MaxArchiveCompressionRatio = 200;
}
