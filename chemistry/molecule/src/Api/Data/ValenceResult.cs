namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by valence.
/// </summary>
public class ValenceResult
{
    /// <summary>
    /// The atom identifier.
    /// </summary>
    public int AtomId;
    /// <summary>
    /// The element's atomic number; <c>0</c> represents an unspecified or query atom.
    /// </summary>
    public int AtomicNumber;
    /// <summary>
    /// The bond valence.
    /// </summary>
    public int BondValence;
    /// <summary>
    /// The hydrogen valence.
    /// </summary>
    public int HydrogenValence;
    /// <summary>
    /// The total valence.
    /// </summary>
    public int TotalValence;
    /// <summary>
    /// The electron valence.
    /// </summary>
    public int ElectronValence;
    /// <summary>
    /// The formal charge.
    /// </summary>
    public int FormalCharge;
    /// <summary>
    /// The radical electron count.
    /// </summary>
    public int RadicalElectronCount;
    /// <summary>
    /// The dative bond count.
    /// </summary>
    public int DativeBondCount;
    /// <summary>
    /// The aromatic bond count.
    /// </summary>
    public int AromaticBondCount;
    /// <summary>
    /// The maximum known valence.
    /// </summary>
    public int MaximumKnownValence;
    /// <summary>
    /// Whether the value is allowed.
    /// </summary>
    public bool IsAllowed;
    /// <summary>
    /// Whether the value is table allowed.
    /// </summary>
    public bool IsTableAllowed;
    /// <summary>
    /// Whether the value is charge adjusted allowed.
    /// </summary>
    public bool IsChargeAdjustedAllowed;
    /// <summary>
    /// Whether the value is radical adjusted allowed.
    /// </summary>
    public bool IsRadicalAdjustedAllowed;
    /// <summary>
    /// Whether the value is aromatic allowed.
    /// </summary>
    public bool IsAromaticAllowed;
    /// <summary>
    /// Whether the value is hypervalent allowed.
    /// </summary>
    public bool IsHypervalentAllowed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValenceResult"/> class.
    /// </summary>
    public ValenceResult()
    {
        AtomId = -1;
        AtomicNumber = 0;
        BondValence = 0;
        HydrogenValence = 0;
        TotalValence = 0;
        ElectronValence = 0;
        FormalCharge = 0;
        RadicalElectronCount = 0;
        DativeBondCount = 0;
        AromaticBondCount = 0;
        MaximumKnownValence = 0;
        IsAllowed = false;
        IsTableAllowed = false;
        IsChargeAdjustedAllowed = false;
        IsRadicalAdjustedAllowed = false;
        IsAromaticAllowed = false;
        IsHypervalentAllowed = false;
    }
}

