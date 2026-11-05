using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by aromaticity perception.
/// </summary>
public class AromaticityPerceptionResult
{
    /// <summary>
    /// The molecule.
    /// </summary>
    public Molecule Molecule;
    /// <summary>
    /// Diagnostics produced while processing the value.
    /// </summary>
    public MoleculeReport Report;
    internal AromaticityCandidateSet Candidates;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;
    /// <summary>
    /// The aromatic ring identifiers.
    /// </summary>
    public List<int> AromaticRingIds;
    /// <summary>
    /// The aromatic atom identifiers.
    /// </summary>
    public List<int> AromaticAtomIds;
    /// <summary>
    /// The aromatic bond identifiers.
    /// </summary>
    public List<int> AromaticBondIds;
    /// <summary>
    /// The aromaticity model applied by the operation.
    /// </summary>
    public AromaticityModelKind Model;
    /// <summary>
    /// Ring identifiers rejected by the selected aromaticity model.
    /// </summary>
    public List<int> RejectedRingIds;
    /// <summary>
    /// Whether a rejected conjugated system used the deterministic nonaromatic fallback.
    /// </summary>
    public bool UsedFallback;

    /// <summary>
    /// Initializes a new instance of the <see cref="AromaticityPerceptionResult"/> class.
    /// </summary>
    public AromaticityPerceptionResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        Candidates = new AromaticityCandidateSet();
        Success = false;
        AromaticRingIds = new List<int>();
        AromaticAtomIds = new List<int>();
        AromaticBondIds = new List<int>();
        Model = AromaticityModelKind.DaylightLike;
        RejectedRingIds = new List<int>();
        UsedFallback = false;
    }

    /// <summary>
    /// Ring is aromatic.
    /// </summary>
    /// <param name="ringId">The ring identifier.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool RingIsAromatic(int ringId)
    {
        return ContainsInt(AromaticRingIds, ringId);
    }

    /// <summary>
    /// Atom is aromatic.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool AtomIsAromatic(int atomId)
    {
        return ContainsInt(AromaticAtomIds, atomId);
    }

    /// <summary>
    /// Bond is aromatic.
    /// </summary>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool BondIsAromatic(int bondId)
    {
        return ContainsInt(AromaticBondIds, bondId);
    }

    internal bool ContainsInt(List<int> values, int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
