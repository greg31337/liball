using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Records the scoring terms used to choose a canonical tautomer.
/// </summary>
public class TautomerScore
{
    /// <summary>
    /// The candidate's zero-based index in the enumeration result.
    /// </summary>
    public int TautomerIndex;
    /// <summary>
    /// The combined score used for ordering candidates.
    /// </summary>
    public int TotalScore;
    /// <summary>
    /// The number of carbonyl bonds contributing to the score.
    /// </summary>
    public int CarbonylBondCount;
    /// <summary>
    /// The number of double bonds to heteroatoms contributing to the score.
    /// </summary>
    public int HeteroDoubleBondCount;
    /// <summary>
    /// The number of aromatic bonds contributing to the score.
    /// </summary>
    public int AromaticBondCount;
    /// <summary>
    /// The penalty derived from the magnitude of formal charges.
    /// </summary>
    public int FormalChargePenalty;
    /// <summary>
    /// The penalty for hydrogens attached to heteroatoms.
    /// </summary>
    public int HeteroHydrogenPenalty;
    /// <summary>
    /// The penalty for unpaired electrons.
    /// </summary>
    public int RadicalPenalty;
    /// <summary>
    /// The deterministic structural key used when numeric scores tie.
    /// </summary>
    public string TieBreakKey;

    /// <summary>
    /// Initializes an empty score whose candidate index is <c>-1</c>.
    /// </summary>
    public TautomerScore()
    {
        TautomerIndex = -1;
        TotalScore = 0;
        CarbonylBondCount = 0;
        HeteroDoubleBondCount = 0;
        AromaticBondCount = 0;
        FormalChargePenalty = 0;
        HeteroHydrogenPenalty = 0;
        RadicalPenalty = 0;
        TieBreakKey = "";
    }
}

