namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by hydrogen calculation.
/// </summary>
public class HydrogenCalculationResult
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
    /// The target valence.
    /// </summary>
    public int TargetValence;
    /// <summary>
    /// The existing implicit hydrogen count.
    /// </summary>
    public int ExistingImplicitHydrogenCount;
    /// <summary>
    /// The spec explicit hydrogen count.
    /// </summary>
    public int SpecExplicitHydrogenCount;
    /// <summary>
    /// The graph explicit hydrogen count.
    /// </summary>
    public int GraphExplicitHydrogenCount;
    /// <summary>
    /// The calculated implicit hydrogen count.
    /// </summary>
    public int CalculatedImplicitHydrogenCount;
    /// <summary>
    /// The total hydrogen count.
    /// </summary>
    public int TotalHydrogenCount;
    /// <summary>
    /// Whether the value is known element.
    /// </summary>
    public bool IsKnownElement;
    /// <summary>
    /// Whether the requested atom identifier exists in the molecule.
    /// </summary>
    public bool IsValidAtom;
    /// <summary>
    /// Whether the atom's occupied valence permits the calculated hydrogen count.
    /// </summary>
    public bool IsValidValence;

    /// <summary>
    /// Initializes a new instance of the <see cref="HydrogenCalculationResult"/> class.
    /// </summary>
    public HydrogenCalculationResult()
    {
        AtomId = -1;
        AtomicNumber = 0;
        BondValence = 0;
        TargetValence = 0;
        ExistingImplicitHydrogenCount = 0;
        SpecExplicitHydrogenCount = 0;
        GraphExplicitHydrogenCount = 0;
        CalculatedImplicitHydrogenCount = 0;
        TotalHydrogenCount = 0;
        IsKnownElement = false;
        IsValidAtom = false;
        IsValidValence = false;
    }
}
