namespace asn1;

/// <summary>
/// Identifies a half-open range within one source unit.
/// </summary>
/// <remarks>
/// Start is inclusive and End is exclusive. Both positions use the same source name and End does not precede Start.
/// </remarks>
public class AsnSourceSpan
{
    /// <summary>
    /// Contains the inclusive start position.
    /// </summary>
    public AsnSourcePosition Start = new AsnSourcePosition();

    /// <summary>
    /// Contains the exclusive end position.
    /// </summary>
    public AsnSourcePosition End = new AsnSourcePosition();
}
