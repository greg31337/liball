using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides cip double bond ranking data and operations for molecular graphs.
/// </summary>
public class CipDoubleBondRanking
{
    /// <summary>
    /// The stereo record identifier.
    /// </summary>
    public int StereoRecordId;
    /// <summary>
    /// The stereo bond identifier.
    /// </summary>
    public int StereoBondId;
    /// <summary>
    /// The begin atom identifier.
    /// </summary>
    public int BeginAtomId;
    /// <summary>
    /// The end atom identifier.
    /// </summary>
    public int EndAtomId;
    /// <summary>
    /// The begin top ligand atom identifier.
    /// </summary>
    public int BeginTopLigandAtomId;
    /// <summary>
    /// The end top ligand atom identifier.
    /// </summary>
    public int EndTopLigandAtomId;
    /// <summary>
    /// The begin ligands.
    /// </summary>
    public List<CipLigandRank> BeginLigands;
    /// <summary>
    /// The end ligands.
    /// </summary>
    public List<CipLigandRank> EndLigands;
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
    /// Initializes a new instance of the <see cref="CipDoubleBondRanking"/> class.
    /// </summary>
    public CipDoubleBondRanking()
    {
        StereoRecordId = -1;
        StereoBondId = -1;
        BeginAtomId = -1;
        EndAtomId = -1;
        BeginTopLigandAtomId = -1;
        EndTopLigandAtomId = -1;
        BeginLigands = new List<CipLigandRank>();
        EndLigands = new List<CipLigandRank>();
        IsValid = false;
        Label = "";
        Report = new MoleculeReport();
    }
}

