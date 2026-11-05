namespace cbor;

/// <summary>
/// Provides registered media types and structured syntax suffixes for CBOR data.
/// </summary>
public static class CborMediaTypes
{
    /// <summary>The media type for one encoded CBOR data item.</summary>
    public const string Cbor = "application/cbor";
    /// <summary>The media type for a CBOR Sequence.</summary>
    public const string CborSequence = "application/cbor-seq";
    /// <summary>The structured syntax suffix for one encoded CBOR data item.</summary>
    public const string CborStructuredSyntaxSuffix = "+cbor";
    /// <summary>The structured syntax suffix for a CBOR Sequence.</summary>
    public const string CborSequenceStructuredSyntaxSuffix = "+cbor-seq";
}
