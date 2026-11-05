namespace molecule;


/// <summary>
/// Describes one bond-order proposal and whether it was applied.
/// </summary>
public class BondOrderAssignment
{
    /// <summary>
    /// The bond identifier.
    /// </summary>
    public int BondId;
    /// <summary>
    /// The order present before perception.
    /// </summary>
    public BondOrder PreviousOrder;
    /// <summary>
    /// The order proposed by perception.
    /// </summary>
    public BondOrder PerceivedOrder;
    /// <summary>
    /// The confidence assigned to the proposal.
    /// </summary>
    public BondOrderConfidence Confidence;
    /// <summary>
    /// The measured bond distance after applying the configured coordinate scale.
    /// </summary>
    public double Distance;
    /// <summary>
    /// The sum of the endpoint covalent radii used as the expected single-bond length.
    /// </summary>
    public double ReferenceSingleBondLength;
    /// <summary>
    /// Whether the proposal was applied to the returned molecule.
    /// </summary>
    public bool Applied;
    /// <summary>
    /// Whether the available data did not distinguish a safe assignment.
    /// </summary>
    public bool IsAmbiguous;
    /// <summary>
    /// A stable explanation of the assignment decision.
    /// </summary>
    public string Reason;

    /// <summary>
    /// Initializes an unresolved assignment.
    /// </summary>
    public BondOrderAssignment()
    {
        BondId = -1;
        PreviousOrder = BondOrder.Unknown;
        PerceivedOrder = BondOrder.Unknown;
        Confidence = BondOrderConfidence.None;
        Distance = 0.0;
        ReferenceSingleBondLength = 0.0;
        Applied = false;
        IsAmbiguous = false;
        Reason = "";
    }
}
