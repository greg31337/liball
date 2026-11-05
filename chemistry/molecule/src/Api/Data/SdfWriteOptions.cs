namespace molecule;


/// <summary>
/// Controls embedded molfile output, SDF data fields, and record separators.
/// </summary>
public class SdfWriteOptions
{
    /// <summary>
    /// Embedded molfile options.
    /// </summary>
    public MolfileWriteOptions Molfile;
    /// <summary>
    /// Whether SDF data fields are emitted.
    /// </summary>
    public bool WriteDataFields;
    /// <summary>
    /// Whether each emitted record ends with an SDF separator.
    /// </summary>
    public bool WriteRecordSeparator;

    /// <summary>
    /// Initializes feature-preserving output with record separators.
    /// </summary>
    public SdfWriteOptions()
    {
        Molfile = new MolfileWriteOptions();
        WriteDataFields = true;
        WriteRecordSeparator = true;
    }
}
