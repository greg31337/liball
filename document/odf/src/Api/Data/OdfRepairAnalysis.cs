namespace odf;

/// <summary>
/// Contains explicit repair proposals produced without mutating a document.
/// </summary>
public class OdfRepairAnalysis
{
    /// <summary>
    /// The number of unambiguous proposals that may be applied.
    /// </summary>
    public int SafeProposalCount;
    /// <summary>
    /// The number of proposals retained for manual review.
    /// </summary>
    public int UnsafeProposalCount;
    /// <summary>
    /// The proposed repairs in deterministic analysis order.
    /// </summary>
    public OdfRepairProposal[] Proposals = [];
}
