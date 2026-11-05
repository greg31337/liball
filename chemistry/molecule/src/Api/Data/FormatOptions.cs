namespace molecule;


/// <summary>
/// Selects an input or output format explicitly or supplies a file name for detection.
/// </summary>
public class FormatOptions
{
    /// <summary>
    /// The kind hint.
    /// </summary>
    public FormatKind KindHint;
    /// <summary>
    /// The file name.
    /// </summary>
    public string FileName;
    /// <summary>
    /// Format-specific SMILES parsing options.
    /// </summary>
    public SmilesReadOptions SmilesRead;
    /// <summary>
    /// Format-specific SMILES writing options.
    /// </summary>
    public SmilesWriteOptions SmilesWrite;
    /// <summary>
    /// Policies used by record-oriented readers and writers.
    /// </summary>
    public RecordStreamOptions RecordStream;
    /// <summary>
    /// Format-specific molfile parsing options.
    /// </summary>
    public MolfileReadOptions MolfileRead;
    /// <summary>
    /// Format-specific molfile writing options.
    /// </summary>
    public MolfileWriteOptions MolfileWrite;
    /// <summary>
    /// Format-specific SDF parsing options.
    /// </summary>
    public SdfReadOptions SdfRead;
    /// <summary>
    /// Format-specific SDF writing options.
    /// </summary>
    public SdfWriteOptions SdfWrite;
    /// <summary>
    /// Format-specific delimited vendor writing options.
    /// </summary>
    public VendorWriteOptions VendorWrite;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatOptions"/> class.
    /// </summary>
    public FormatOptions()
    {
        KindHint = FormatKind.Unknown;
        FileName = "";
        SmilesRead = new SmilesReadOptions();
        SmilesWrite = new SmilesWriteOptions();
        RecordStream = new RecordStreamOptions();
        MolfileRead = new MolfileReadOptions();
        MolfileWrite = new MolfileWriteOptions();
        SdfRead = new SdfReadOptions();
        SdfWrite = new SdfWriteOptions();
        VendorWrite = new VendorWriteOptions();
    }
}
