namespace molecule;


/// <summary>
/// Contains molfile output text, selected version, and diagnostics.
/// </summary>
public class MolfileWriteResult
{
    /// <summary>
    /// The emitted molfile text.
    /// </summary>
    public string Text;
    /// <summary>
    /// The emitted molfile version.
    /// </summary>
    public MolfileVersion Version;
    /// <summary>
    /// Diagnostics produced while writing.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// Whether output completed without an error diagnostic.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes an empty molfile write result.
    /// </summary>
    public MolfileWriteResult()
    {
        Text = "";
        Version = MolfileVersion.Auto;
        Report = new MoleculeReport();
        Success = false;
    }
}
