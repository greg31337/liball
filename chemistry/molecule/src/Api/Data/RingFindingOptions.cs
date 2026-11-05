namespace molecule;


/// <summary>
/// Controls deterministic cycle-basis and bounded small-ring searches.
/// </summary>
public class RingFindingOptions
{
    /// <summary>
    /// Whether the result includes a deterministic independent cycle basis.
    /// </summary>
    public bool FindCycleBasis;
    /// <summary>
    /// Whether the result enumerates all simple rings within the configured limits.
    /// </summary>
    public bool FindAllSmallRings;
    /// <summary>
    /// The largest ring size included in all-small-ring enumeration.
    /// </summary>
    public int MaximumRingSize;
    /// <summary>
    /// The maximum number of distinct small rings returned.
    /// </summary>
    public int MaximumSmallRingCount;
    /// <summary>
    /// The maximum number of graph-search steps used for small-ring enumeration.
    /// </summary>
    public int MaximumSearchStepCount;

    /// <summary>
    /// Initializes options for a cycle basis without exhaustive small-ring enumeration.
    /// </summary>
    public RingFindingOptions()
    {
        FindCycleBasis = true;
        FindAllSmallRings = false;
        MaximumRingSize = 8;
        MaximumSmallRingCount = 256;
        MaximumSearchStepCount = 250000;
    }
}
