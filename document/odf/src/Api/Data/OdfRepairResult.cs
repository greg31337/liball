namespace odf;

/// <summary>
/// Reports the outcome of applying selected repair proposals.
/// </summary>
public class OdfRepairResult
{
    /// <summary>
    /// The overall edit status.
    /// </summary>
    public OdfEditStatus Status = OdfEditStatus.Failed;
    /// <summary>
    /// Whether at least one mutation was performed.
    /// </summary>
    public bool Changed;
    /// <summary>
    /// The number of proposals successfully applied.
    /// </summary>
    public int AppliedProposalCount;
    /// <summary>
    /// The number of unsafe, stale, or invalid proposals skipped.
    /// </summary>
    public int SkippedProposalCount;
    /// <summary>
    /// Every mutation performed, including related reference updates.
    /// </summary>
    public OdfRepairMutation[] Mutations = [];
    /// <summary>
    /// Conflict and failure diagnostics produced during application.
    /// </summary>
    public OdfDiagnostic[] Diagnostics = [];
}
