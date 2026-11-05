namespace odf;

/// <summary>
/// Identifies an OpenFormula conformance level.
/// </summary>
public enum OdfFormulaConformance
{
    /// <summary>
    /// The conformance level is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// The OpenFormula Small group.
    /// </summary>
    Small = 1,
    /// <summary>
    /// The OpenFormula Medium group.
    /// </summary>
    Medium = 2,
    /// <summary>
    /// The OpenFormula Large group.
    /// </summary>
    Large = 3
}
