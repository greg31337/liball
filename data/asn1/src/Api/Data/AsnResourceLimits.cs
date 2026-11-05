namespace asn1;

/// <summary>
/// Selects whether codec resource limits use bounded defaults or explicit unlimited behavior.
/// </summary>
/// <remarks>
/// Each maximum is a positive bound when Unlimited is false. Zero and negative values do not request unlimited behavior.
/// </remarks>
public class AsnResourceLimits
{
    /// <summary>
    /// Indicates whether explicitly unlimited behavior was requested.
    /// </summary>
    public bool Unlimited = false;

    /// <summary>
    /// Contains the maximum constructed-value and parser nesting depth.
    /// </summary>
    public int MaximumNestingDepth = 128;

    /// <summary>
    /// Contains the maximum number of elements in one collection.
    /// </summary>
    public int MaximumCollectionElements = 1000000;

    /// <summary>
    /// Contains the maximum number of octets in one encoded tag.
    /// </summary>
    public int MaximumTagOctets = 16;

    /// <summary>
    /// Contains the maximum number of octets in one encoded length determinant.
    /// </summary>
    public int MaximumLengthOctets = 8;

    /// <summary>
    /// Contains the maximum bit count of one arbitrary-size integer.
    /// </summary>
    public int MaximumIntegerBits = 1048576;

    /// <summary>
    /// Contains the maximum character count of one text value or source unit.
    /// </summary>
    public int MaximumTextCharacters = 16777216;

    /// <summary>
    /// Contains the maximum byte count of one retained open-type value.
    /// </summary>
    public int MaximumOpenTypeBytes = 16777216;

    /// <summary>
    /// Contains the maximum total byte allocation charged to one operation.
    /// </summary>
    public long MaximumTotalAllocationBytes = 268435456;

    /// <summary>
    /// Contains the maximum number of diagnostics produced by one operation.
    /// </summary>
    public int MaximumDiagnostics = 10000;
}
