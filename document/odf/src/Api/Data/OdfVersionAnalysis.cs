namespace odf;

/// <summary>
/// Reports version support, migration steps, and possible conversion loss.
/// </summary>
public class OdfVersionAnalysis
{
    /// <summary>
    /// Whether the requested analysis completed without error diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Whether both source and target are known ODF versions.
    /// </summary>
    public bool IsSupported;
    /// <summary>
    /// Whether the requested migration moves to a newer version.
    /// </summary>
    public bool IsUpgrade;
    /// <summary>
    /// Whether the requested migration moves to an older version.
    /// </summary>
    public bool IsDowngrade;
    /// <summary>
    /// Whether conversion can proceed without known content loss.
    /// </summary>
    public bool CanConvertWithoutLoss;
    /// <summary>
    /// Whether unknown future-version content will be preserved.
    /// </summary>
    public bool PreservesFutureContent;
    /// <summary>
    /// Whether the source version was inferred because none was declared.
    /// </summary>
    public bool VersionWasInferred;
    /// <summary>
    /// The detected or inferred source version.
    /// </summary>
    public OdfVersion SourceVersion;
    /// <summary>
    /// The selected target version.
    /// </summary>
    public OdfVersion TargetVersion;
    /// <summary>
    /// The adjacent-version migration steps in execution order.
    /// </summary>
    public OdfVersionMigrationStep[] Steps = [];
    /// <summary>
    /// Version-gate and downgrade-loss diagnostics.
    /// </summary>
    public OdfDiagnostic[] Diagnostics = [];
}
