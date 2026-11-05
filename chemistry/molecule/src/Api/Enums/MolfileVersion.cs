namespace molecule;


/// <summary>
/// Selects automatic, V2000, or V3000 molfile processing.
/// </summary>
public enum MolfileVersion
{
    /// <summary>
    /// Selects a version from input markers or output requirements.
    /// </summary>
    Auto = 0,
    /// <summary>
    /// Selects MOL V2000.
    /// </summary>
    V2000 = 1,
    /// <summary>
    /// Selects MOL V3000.
    /// </summary>
    V3000 = 2
}
