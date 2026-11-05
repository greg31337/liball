using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains extracted PDB ligand molecules, their source residues, counts, and diagnostics.
/// </summary>
public class PdbLigandExtractionResult
{
    /// <summary>
    /// Independently mutable ligand molecular graphs in source residue order.
    /// </summary>
    public List<Molecule> Ligands;
    /// <summary>
    /// The source residue records corresponding to <see cref="Ligands"/>.
    /// </summary>
    public List<Residue> Residues;
    /// <summary>
    /// Diagnostics produced while extracting ligands.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// Whether extraction completed without an error diagnostic.
    /// </summary>
    public bool Success;
    /// <summary>
    /// The number of hetero residues considered after model filtering.
    /// </summary>
    public int CandidateCount;
    /// <summary>
    /// The number of returned ligands.
    /// </summary>
    public int ExtractedCount;
    /// <summary>
    /// The number of water residues skipped by policy.
    /// </summary>
    public int SkippedWaterCount;

    /// <summary>
    /// Initializes an empty ligand extraction result.
    /// </summary>
    public PdbLigandExtractionResult()
    {
        Ligands = new List<Molecule>();
        Residues = new List<Residue>();
        Report = new MoleculeReport();
        Success = false;
        CandidateCount = 0;
        ExtractedCount = 0;
        SkippedWaterCount = 0;
    }
}
