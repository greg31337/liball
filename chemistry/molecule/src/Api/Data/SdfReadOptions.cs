namespace molecule;


/// <summary>
/// Controls SDF record recovery, mixed-version handling, data fields, and embedded molfiles.
/// </summary>
public class SdfReadOptions
{
    /// <summary>
    /// Embedded molfile options.
    /// </summary>
    public MolfileReadOptions Molfile;
    /// <summary>
    /// Whether reading continues after a failed record.
    /// </summary>
    public bool ContinueAfterError;
    /// <summary>
    /// Whether failed records are retained in the result.
    /// </summary>
    public bool IncludeFailedRecords;
    /// <summary>
    /// Whether SDF data fields are preserved in molecule metadata.
    /// </summary>
    public bool ReadDataFields;
    /// <summary>
    /// Whether V2000 and V3000 records may occur in one stream.
    /// </summary>
    public bool AllowMixedMolfileVersions;

    /// <summary>
    /// Initializes continuing, feature-preserving mixed-version SDF options.
    /// </summary>
    public SdfReadOptions()
    {
        Molfile = new MolfileReadOptions();
        ContinueAfterError = true;
        IncludeFailedRecords = true;
        ReadDataFields = true;
        AllowMixedMolfileVersions = true;
    }
}
