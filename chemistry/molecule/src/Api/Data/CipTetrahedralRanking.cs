using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides cip tetrahedral ranking data and operations for molecular graphs.
/// </summary>
public class CipTetrahedralRanking
{
    /// <summary>
    /// The stereo record identifier.
    /// </summary>
    public int StereoRecordId;
    /// <summary>
    /// The center atom identifier.
    /// </summary>
    public int CenterAtomId;
    /// <summary>
    /// The ligands.
    /// </summary>
    public List<CipLigandRank> Ligands;
    /// <summary>
    /// Whether the value is valid.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// The label.
    /// </summary>
    public string Label;
    /// <summary>
    /// Diagnostics produced while processing the value.
    /// </summary>
    public MoleculeReport Report;

    /// <summary>
    /// Initializes a new instance of the <see cref="CipTetrahedralRanking"/> class.
    /// </summary>
    public CipTetrahedralRanking()
    {
        StereoRecordId = -1;
        CenterAtomId = -1;
        Ligands = new List<CipLigandRank>();
        IsValid = false;
        Label = "";
        Report = new MoleculeReport();
    }
}

