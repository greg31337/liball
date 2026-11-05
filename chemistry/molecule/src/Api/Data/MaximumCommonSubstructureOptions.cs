namespace molecule;


/// <summary>
/// Controls atom and bond matching plus deterministic maximum-common-substructure search bounds.
/// </summary>
public class MaximumCommonSubstructureOptions
{
    /// <summary>
    /// Whether isotope mass numbers must match.
    /// </summary>
    public bool MatchIsotopes;
    /// <summary>
    /// Whether formal charges must match.
    /// </summary>
    public bool MatchFormalCharges;
    /// <summary>
    /// Whether explicit and implicit hydrogen counts must match.
    /// </summary>
    public bool MatchHydrogenCounts;
    /// <summary>
    /// Whether radical electron counts must match.
    /// </summary>
    public bool MatchRadicals;
    /// <summary>
    /// Whether atom-map numbers must match.
    /// </summary>
    public bool MatchAtomMaps;
    /// <summary>
    /// Whether aromatic atom and bond flags must match.
    /// </summary>
    public bool MatchAromaticity;
    /// <summary>
    /// Whether bond orders must match.
    /// </summary>
    public bool MatchBondOrders;
    /// <summary>
    /// Whether bond stereo and direction must match.
    /// </summary>
    public bool MatchStereo;
    /// <summary>
    /// Whether every returned atom must belong to one connected common component.
    /// </summary>
    public bool RequireConnected;
    /// <summary>
    /// The minimum number of mapped atoms required for <c>Found</c> to be true.
    /// </summary>
    public int MinimumAtomCount;
    /// <summary>
    /// The maximum number of backtracking states visited.
    /// </summary>
    public int MaximumSearchStepCount;
    /// <summary>
    /// The maximum elapsed search time in milliseconds; <c>0</c> requests an immediate timeout.
    /// </summary>
    public int MaximumElapsedMilliseconds;

    /// <summary>
    /// Initializes a connected element, aromaticity, and bond-order query with finite work and time limits.
    /// </summary>
    public MaximumCommonSubstructureOptions()
    {
        MatchIsotopes = false;
        MatchFormalCharges = false;
        MatchHydrogenCounts = false;
        MatchRadicals = false;
        MatchAtomMaps = false;
        MatchAromaticity = true;
        MatchBondOrders = true;
        MatchStereo = false;
        RequireConnected = true;
        MinimumAtomCount = 1;
        MaximumSearchStepCount = 250000;
        MaximumElapsedMilliseconds = 10000;
    }
}
