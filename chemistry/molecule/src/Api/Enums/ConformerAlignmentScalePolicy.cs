namespace molecule;


/// <summary>
/// Specifies whether conformer alignment preserves size or performs a uniform best-fit scale.
/// </summary>
public enum ConformerAlignmentScalePolicy
{
    /// <summary>
    /// Applies only rotation and translation.
    /// </summary>
    Preserve = 0,
    /// <summary>
    /// Applies the positive uniform scale that minimizes mapped-coordinate squared error.
    /// </summary>
    BestFit = 1
}
