namespace odf;

/// <summary>
/// Describes the outcome of an editing operation.
/// </summary>
public class OdfEditResult
{
    /// <summary>
    /// The edit status.
    /// </summary>
    public OdfEditStatus Status = OdfEditStatus.Failed;
    /// <summary>
    /// Whether the operation changed the document.
    /// </summary>
    public bool Changed;
    /// <summary>
    /// The number of affected document items.
    /// </summary>
    public int AffectedCount;
    /// <summary>
    /// A human-readable result message, or an empty string.
    /// </summary>
    public string Message = "";
    /// <summary>
    /// The primary location affected by the edit.
    /// </summary>
    public OdfLocation Location = new OdfLocation();
    /// <summary>
    /// Diagnostics produced by the edit.
    /// </summary>
    public OdfDiagnostic[] Diagnostics = [];
    /// <summary>
    /// Search or replacement matches associated with the edit.
    /// </summary>
    public OdfSearchMatch[] SearchMatches = [];
    /// <summary>
    /// The mutations committed by an edit session.
    /// </summary>
    public OdfEditMutation[] Mutations = [];
}
