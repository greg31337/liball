namespace asn1;

/// <summary>
/// Describes one normalized arbitrary-integer interval.
/// </summary>
public class AsnIntervalDescriptor
{
    /// <summary>
    /// Contains the stable interval identity.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Indicates whether a finite lower bound is present.
    /// </summary>
    public bool HasLowerBound;

    /// <summary>
    /// Contains the lower bound when present.
    /// </summary>
    public System.Numerics.BigInteger LowerBound;

    /// <summary>
    /// Indicates whether a finite upper bound is present.
    /// </summary>
    public bool HasUpperBound;

    /// <summary>
    /// Contains the upper bound when present.
    /// </summary>
    public System.Numerics.BigInteger UpperBound;

    /// <summary>
    /// Contains the interval source span when available.
    /// </summary>
    public AsnSourceSpan? SourceSpan;
}
