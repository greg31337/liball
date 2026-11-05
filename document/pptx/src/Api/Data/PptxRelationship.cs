namespace pptx;

/// <summary>
/// Represents relationship data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxRelationship
{
    /// <summary>
    /// Stores the source part name associated with this object.
    /// </summary>
    public string SourcePartName;
    /// <summary>
    /// Stores the id used to identify or connect related presentation data.
    /// </summary>
    public string Id;
    /// <summary>
    /// Stores the type associated with this object.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores the target associated with this object.
    /// </summary>
    public string Target;
    /// <summary>
    /// Stores the target mode value associated with this object.
    /// </summary>
    public PptxRelationshipTargetMode TargetMode;
    /// <summary>
    /// Stores the target part name associated with this object.
    /// </summary>
    public string TargetPartName;
    /// <summary>
    /// Stores the target fragment associated with this object.
    /// </summary>
    public string TargetFragment;

    /// <summary>
    /// Initializes a resolved package relationship and retains both its stored target and resolved internal target details.
    /// </summary>
    /// <param name="sourcePartName">The owning part name, or an empty string for package-level relationships.</param>
    /// <param name="id">The relationship identifier referenced by the owning part.</param>
    /// <param name="type">The Open Packaging Convention relationship type URI.</param>
    /// <param name="target">The target text exactly as stored in the relationships part.</param>
    /// <param name="targetMode">Whether the target is another package part or an external resource.</param>
    /// <param name="targetPartName">The normalized internal target part name, or an empty string for external targets.</param>
    /// <param name="targetFragment">The target URI fragment without the leading hash, or an empty string.</param>
    public PptxRelationship(
        string sourcePartName,
        string id,
        string type,
        string target,
        PptxRelationshipTargetMode targetMode,
        string targetPartName,
        string targetFragment)
    {
        SourcePartName = PptxDiagnostic.CleanText(sourcePartName);
        Id = PptxDiagnostic.CleanText(id);
        Type = PptxDiagnostic.CleanText(type);
        Target = PptxDiagnostic.CleanText(target);
        TargetMode = targetMode;
        TargetPartName = PptxDiagnostic.CleanText(targetPartName);
        TargetFragment = PptxDiagnostic.CleanText(targetFragment);
    }
    internal bool IsExternal()
    {
        return TargetMode == PptxRelationshipTargetMode.External;
    }
    internal bool IsInternal()
    {
        return TargetMode == PptxRelationshipTargetMode.Internal;
    }
}

