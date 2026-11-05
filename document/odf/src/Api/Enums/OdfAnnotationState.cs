namespace odf;

/// <summary>
/// Identifies the workflow state of an annotation.
/// </summary>
public enum OdfAnnotationState
{
    /// <summary>
    /// The annotation state is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// The annotation remains open.
    /// </summary>
    Open = 1,
    /// <summary>
    /// The annotation has been resolved.
    /// </summary>
    Resolved = 2
}
