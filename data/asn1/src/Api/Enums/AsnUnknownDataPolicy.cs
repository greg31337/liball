namespace asn1;

/// <summary>
/// Identifies how a codec handles unknown extension or open data.
/// </summary>
public enum AsnUnknownDataPolicy
{
    /// <summary>
    /// Represents reject.
    /// </summary>
    Reject = 0,

    /// <summary>
    /// Represents preserve.
    /// </summary>
    Preserve = 1,

    /// <summary>
    /// Represents skip.
    /// </summary>
    Skip = 2
}
