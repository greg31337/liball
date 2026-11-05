namespace molecule;


/// <summary>
/// Contains SDF output text, diagnostics, and emitted record count.
/// </summary>
public class SdfWriteResult
{
    /// <summary>
    /// The emitted SDF text.
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
    /// The number of emitted records.
    /// </summary>
    public int WrittenCount;

    /// <summary>
    /// Initializes an empty SDF write result.
    /// </summary>
    public SdfWriteResult()
    {
        Text = "";
        Report = new MoleculeReport();
        Success = false;
        WrittenCount = 0;
    }
}
