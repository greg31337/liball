namespace icc;

/// <summary>
/// Identifies a lookup-table interpolation algorithm.
/// </summary>
public enum IccInterpolationKind
{
    /// <summary>
    /// Identifies one-dimensional linear interpolation.
    /// </summary>
    Linear = 0,

    /// <summary>
    /// Identifies N-dimensional multilinear interpolation.
    /// </summary>
    Multilinear = 1,

    /// <summary>
    /// Identifies three-dimensional tetrahedral interpolation.
    /// </summary>
    Tetrahedral = 2
}
