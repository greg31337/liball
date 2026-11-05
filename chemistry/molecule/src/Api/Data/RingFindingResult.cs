namespace molecule;


/// <summary>
/// Contains deterministic ring sets, membership, systems, limits, and diagnostics.
/// </summary>
public class RingFindingResult
{
    /// <summary>
    /// The independent deterministic cycle basis.
    /// </summary>
    public RingSet CycleBasis;
    /// <summary>
    /// All distinct simple rings found within the caller's limits.
    /// </summary>
    public RingSet AllSmallRings;
    /// <summary>
    /// The combined rings, atom and bond membership, and ring systems requested by the caller.
    /// </summary>
    public RingSet Rings;
    /// <summary>
    /// Diagnostics produced during ring finding.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// The number of graph-search steps used for all-small-ring enumeration.
    /// </summary>
    public int SearchStepCount;
    /// <summary>
    /// Whether enumeration stopped at the ring-count or graph-search limit.
    /// </summary>
    public bool LimitReached;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes an empty successful ring-finding result.
    /// </summary>
    public RingFindingResult()
    {
        CycleBasis = new RingSet();
        AllSmallRings = new RingSet();
        Rings = new RingSet();
        Report = new MoleculeReport();
        SearchStepCount = 0;
        LimitReached = false;
        Success = true;
    }
}
