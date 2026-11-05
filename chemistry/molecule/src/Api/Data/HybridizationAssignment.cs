using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides hybridization assignment data and operations for molecular graphs.
/// </summary>
public class HybridizationAssignment
{
    /// <summary>
    /// The atoms in identifier order; each atom identifier is its list index.
    /// </summary>
    public List<HybridizationResult> Atoms;

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridizationAssignment"/> class.
    /// </summary>
    public HybridizationAssignment()
    {
        Atoms = new List<HybridizationResult>();
    }

    /// <summary>
    /// Returns the atom.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    internal HybridizationResult GetAtom(int atomId)
    {
        int index = 0;
        while (index < Atoms.Count)
        {
            if (Atoms[index].AtomId == atomId)
            {
                return Atoms[index];
            }

            index = index + 1;
        }

        return new HybridizationResult();
    }
}

