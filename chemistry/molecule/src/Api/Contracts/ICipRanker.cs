using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="CipRanker"/>.
/// </summary>
public interface ICipRanker
{
    /// <summary>
    /// Rank tetrahedral.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="record">The record.</param>
    /// <returns>The resulting CIP tetrahedral ranking.</returns>
    CipTetrahedralRanking RankTetrahedral(Molecule? molecule, StereoRecord? record);

    /// <summary>
    /// Rank double bond.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="record">The record.</param>
    /// <returns>The resulting CIP double bond ranking.</returns>
    CipDoubleBondRanking RankDoubleBond(Molecule? molecule, StereoRecord? record);
}

