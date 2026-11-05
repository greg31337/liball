namespace molecule;


/// <summary>
/// Contains PDB output text, emitted record counts, and diagnostics.
/// </summary>
public class PdbWriteResult
{
    /// <summary>
    /// The emitted PDB text.
    /// </summary>
    public string Text;
    /// <summary>
    /// Diagnostics produced while writing.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// Whether output completed without an error diagnostic.
    /// </summary>
    public bool Success;
    /// <summary>
    /// The number of emitted atom records.
    /// </summary>
    public int AtomRecordCount;
    /// <summary>
    /// The number of emitted connectivity records.
    /// </summary>
    public int ConectRecordCount;
    /// <summary>
    /// The number of emitted model blocks.
    /// </summary>
    public int ModelCount;
    /// <summary>
    /// The number of typed residues represented by the molecule.
    /// </summary>
    public int ResidueCount;
    /// <summary>
    /// The number of typed chains represented by the molecule.
    /// </summary>
    public int ChainCount;

    /// <summary>
    /// Initializes an empty PDB write result.
    /// </summary>
    public PdbWriteResult()
    {
        Text = "";
        Report = new MoleculeReport();
        Success = false;
        AtomRecordCount = 0;
        ConectRecordCount = 0;
        ModelCount = 0;
        ResidueCount = 0;
        ChainCount = 0;
    }
}
