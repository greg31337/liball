namespace odf;

internal class RepairApplicationResult
{
    internal OdfEditStatus Status = OdfEditStatus.Failed;
    internal bool Changed;
    internal int AppliedProposalCount;
    internal int SkippedProposalCount;
    internal RepairMutation[] Mutations = [];
    internal OdfDiagnostic[] Diagnostics = [];
}
