namespace molecule;


/// <summary>
/// Specifies how two-dimensional coordinate normalization scales a molecule.
/// </summary>
public enum CoordinateNormalizationScalePolicy
{
    /// <summary>
    /// Preserves all two-dimensional distances.
    /// </summary>
    Preserve = 0,
    /// <summary>
    /// Uniformly scales the mean measurable bond length to a requested value.
    /// </summary>
    AverageBondLength = 1
}
