namespace odf;

/// <summary>
/// Describes one adjacent-version step in a requested ODF migration.
/// </summary>
public class OdfVersionMigrationStep
{
    /// <summary>
    /// The source version for this step.
    /// </summary>
    public OdfVersion SourceVersion;
    /// <summary>
    /// The target version for this step.
    /// </summary>
    public OdfVersion TargetVersion;
    /// <summary>
    /// Whether this step moves to an older version.
    /// </summary>
    public bool IsDowngrade;
    /// <summary>
    /// The number of document rules added in the newer version.
    /// </summary>
    public int AddedCount;
    /// <summary>
    /// The number of document rules removed in the newer version.
    /// </summary>
    public int RemovedCount;
    /// <summary>
    /// The number of document rules changed between versions.
    /// </summary>
    public int ChangedCount;
    /// <summary>
    /// The number of incompatible constructs found in the document.
    /// </summary>
    public int IncompatibleConstructCount;
    /// <summary>
    /// The complete schema-rule changes for this adjacent-version step.
    /// </summary>
    public OdfVersionChange[] Changes = [];
}
