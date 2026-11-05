namespace molecule;


/// <summary>
/// Stores the parsed fields for one sdf record.
/// </summary>
public class SdfRecord
{
    /// <summary>
    /// The index.
    /// </summary>
    public int Index;
    /// <summary>
    /// The molecule.
    /// </summary>
    public Molecule Molecule;
    /// <summary>
    /// Diagnostics produced while processing the value.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;
    /// <summary>
    /// Whether no additional records are available.
    /// </summary>
    public bool EndOfStream;
    /// <summary>
    /// Whether the value has record separator.
    /// </summary>
    public bool HasRecordSeparator;
    /// <summary>
    /// The raw text.
    /// </summary>
    public string RawText;
    /// <summary>
    /// The embedded molfile version.
    /// </summary>
    public MolfileVersion Version;
    /// <summary>
    /// The number of SDF data fields read from this record.
    /// </summary>
    public int DataFieldCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="SdfRecord"/> class.
    /// </summary>
    public SdfRecord()
    {
        Index = -1;
        Molecule = new Molecule();
        Report = new MoleculeReport();
        Success = false;
        EndOfStream = false;
        HasRecordSeparator = false;
        RawText = "";
        Version = MolfileVersion.Auto;
        DataFieldCount = 0;
    }
}
