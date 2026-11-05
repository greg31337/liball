using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Describes one ligand's deterministic priority around a stereochemical center.
/// </summary>
public class CipLigandRank
{
    /// <summary>
    /// The ligand atom identifier, or <c>-1</c> for an implicit ligand.
    /// </summary>
    public int AtomId;
    /// <summary>
    /// The ligand's position in the source stereo record.
    /// </summary>
    public int OriginalIndex;
    /// <summary>
    /// The assigned priority rank, where lower values have higher priority.
    /// </summary>
    public int Rank;
    /// <summary>
    /// The ligand atom's atomic number.
    /// </summary>
    public int AtomicNumber;
    /// <summary>
    /// The ligand atom's isotope mass number, or <c>0</c> when unspecified.
    /// </summary>
    public int IsotopeMassNumber;
    /// <summary>
    /// The comparison score for the first graph sphere around the ligand.
    /// </summary>
    public int FirstSphereScore;
    /// <summary>
    /// The comparison score for the second graph sphere around the ligand.
    /// </summary>
    public int SecondSphereScore;
    /// <summary>
    /// Whether the value is implicit.
    /// </summary>
    public bool IsImplicit;

    /// <summary>
    /// Initializes a ligand ranking with no assigned atom or priority.
    /// </summary>
    public CipLigandRank()
    {
        AtomId = -1;
        OriginalIndex = -1;
        Rank = 0;
        AtomicNumber = 0;
        IsotopeMassNumber = 0;
        FirstSphereScore = 0;
        SecondSphereScore = 0;
        IsImplicit = false;
    }
}

