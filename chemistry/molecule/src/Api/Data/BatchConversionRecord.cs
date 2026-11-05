namespace molecule;


/// <summary>
/// Contains one successfully read batch input record for filtering.
/// </summary>
public class BatchConversionRecord
{
    /// <summary>
    /// The zero-based position of the record in the input stream.
    /// </summary>
    public int Index;
    /// <summary>
    /// The molecule read from the input stream.
    /// </summary>
    public Molecule Molecule;
    /// <summary>
    /// Diagnostics produced while reading the record.
    /// </summary>
    public MoleculeReport Report;

    /// <summary>
    /// Initializes an empty record with an invalid index.
    /// </summary>
    public BatchConversionRecord()
    {
        Index = -1;
        Molecule = new Molecule();
        Report = new MoleculeReport();
    }
}
