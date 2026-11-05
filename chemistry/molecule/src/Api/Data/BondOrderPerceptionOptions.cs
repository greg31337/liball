namespace molecule;


/// <summary>
/// Controls conservative bond-order perception from molecular coordinates.
/// </summary>
public class BondOrderPerceptionOptions
{
    /// <summary>
    /// The coordinate dimension to use.
    /// </summary>
    public BondOrderCoordinateDimension CoordinateDimension;
    /// <summary>
    /// The conformer identifier to use, or <c>-1</c> to use coordinates stored directly on atoms.
    /// </summary>
    public int ConformerId;
    /// <summary>
    /// A multiplier that converts stored coordinate distances to angstroms.
    /// </summary>
    public double CoordinateScale;
    /// <summary>
    /// Whether existing single, double, triple, and quadruple bond orders are left unchanged.
    /// </summary>
    public bool PreserveAssignedBondOrders;
    /// <summary>
    /// The largest relative deviation from an expected bond length that can produce an assignment.
    /// </summary>
    public double MaximumRelativeError;
    /// <summary>
    /// The minimum relative-error separation required between the best and next-best order.
    /// </summary>
    public double MinimumAlternativeGap;
    /// <summary>
    /// The minimum confidence that may be applied to the returned molecule.
    /// </summary>
    public BondOrderConfidence MinimumConfidenceToApply;

    /// <summary>
    /// Initializes conservative defaults for angstrom coordinates stored directly on atoms.
    /// </summary>
    public BondOrderPerceptionOptions()
    {
        CoordinateDimension = BondOrderCoordinateDimension.Automatic;
        ConformerId = -1;
        CoordinateScale = 1.0;
        PreserveAssignedBondOrders = true;
        MaximumRelativeError = 0.12;
        MinimumAlternativeGap = 0.035;
        MinimumConfidenceToApply = BondOrderConfidence.Medium;
    }
}
