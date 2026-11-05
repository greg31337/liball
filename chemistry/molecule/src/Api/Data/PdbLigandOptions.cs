namespace molecule;


/// <summary>
/// Controls extraction of ligand residues from PDB molecular graphs.
/// </summary>
public class PdbLigandOptions
{
    /// <summary>
    /// Whether water residues are returned as ligands.
    /// </summary>
    public bool IncludeWater;
    /// <summary>
    /// Whether single-atom hetero residues, including ions, are returned.
    /// </summary>
    public bool IncludeSingleAtomResidues;
    /// <summary>
    /// The required model identifier, or <c>-1</c> to inspect every model.
    /// </summary>
    public int ModelId;

    /// <summary>
    /// Initializes ligand extraction that excludes water and includes single-atom residues from every model.
    /// </summary>
    public PdbLigandOptions()
    {
        IncludeWater = false;
        IncludeSingleAtomResidues = true;
        ModelId = -1;
    }
}
