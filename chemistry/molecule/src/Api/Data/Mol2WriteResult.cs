namespace molecule;


/// <summary>
/// Contains MOL2 output text, section counts, molecule fields, and diagnostics.
/// </summary>
public class Mol2WriteResult
{
    /// <summary>
    /// The emitted MOL2 text.
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
    /// The number of emitted atoms.
    /// </summary>
    public int AtomCount;
    /// <summary>
    /// The number of emitted bonds.
    /// </summary>
    public int BondCount;
    /// <summary>
    /// The number of emitted substructures.
    /// </summary>
    public int SubstructureCount;
    /// <summary>
    /// The emitted Tripos molecule type.
    /// </summary>
    public string MoleculeType;
    /// <summary>
    /// The emitted Tripos charge type.
    /// </summary>
    public string ChargeType;

    /// <summary>
    /// Initializes an empty MOL2 write result.
    /// </summary>
    public Mol2WriteResult()
    {
        Text = "";
        Report = new MoleculeReport();
        Success = false;
        AtomCount = 0;
        BondCount = 0;
        SubstructureCount = 0;
        MoleculeType = "";
        ChargeType = "";
    }
}
