namespace molecule;


/// <summary>
/// Selects how coordinate distances are measured during bond-order perception.
/// </summary>
public enum BondOrderCoordinateDimension
{
    /// <summary>
    /// Uses three dimensions when any selected coordinate has a nonzero z value; otherwise, uses two dimensions.
    /// </summary>
    Automatic = 0,
    /// <summary>
    /// Uses x and y values and ignores z values.
    /// </summary>
    TwoDimensional = 2,
    /// <summary>
    /// Uses x, y, and z values.
    /// </summary>
    ThreeDimensional = 3
}
