namespace fake;

/// <summary>
/// Selects how generated contactable and allocated-looking values are constrained.
/// </summary>
public enum SafetyMode
{
    /// <summary>
    /// Prefers officially reserved or documentation-only ranges.
    /// </summary>
    Reserved = 0,

    /// <summary>
    /// Produces plausible synthetic shapes without claiming allocation or reachability.
    /// </summary>
    SyntheticPlausible = 1,

    /// <summary>
    /// Allows unrestricted shapes while still making no allocation claim.
    /// </summary>
    UnrestrictedShape = 2
}
