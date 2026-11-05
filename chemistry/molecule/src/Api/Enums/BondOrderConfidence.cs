namespace molecule;


/// <summary>
/// Describes confidence in a bond-order proposal derived from coordinates.
/// </summary>
public enum BondOrderConfidence
{
    /// <summary>
    /// No usable proposal was produced.
    /// </summary>
    None = 0,
    /// <summary>
    /// The proposal is weak and is not applied by default.
    /// </summary>
    Low = 1,
    /// <summary>
    /// The proposal is supported by a distinct, plausible bond length.
    /// </summary>
    Medium = 2,
    /// <summary>
    /// The proposal closely matches one expected bond length and is well separated from alternatives.
    /// </summary>
    High = 3
}
