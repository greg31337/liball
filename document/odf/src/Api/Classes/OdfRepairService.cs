namespace odf;

/// <summary>
/// Provides non-mutating repair analysis and explicit safe repair application.
/// </summary>
public class OdfRepairService : IOdfRepairService
{
    /// <summary>
    /// Initializes a document repair service.
    /// </summary>
    public OdfRepairService()
    {
    }

    /// <summary>
    /// Produces repair proposals without mutating the document.
    /// </summary>
    /// <param name="document">The document to analyze.</param>
    /// <returns>The safe and unsafe repair proposals.</returns>
    public OdfRepairAnalysis ProposeRepairs(OdfDocument document)
    {
        RepairAnalysisResult analysis = new RepairEngine()
            .ProposeRepairs(document);
        return ConvertRepairAnalysis(analysis);
    }

    /// <summary>
    /// Applies selected safe proposals when their analyzed state is still current.
    /// </summary>
    /// <param name="document">The document to repair.</param>
    /// <param name="proposals">Original proposals returned for the document.</param>
    /// <returns>The application result and mutation report.</returns>
    public OdfRepairResult ApplyRepairs(
        OdfDocument document,
        OdfRepairProposal[] proposals)
    {
        if (proposals == null)
        {
            throw new OdfValidationException(
                "Repair proposals are required for repair application.");
        }
        RepairProposal[] coreProposals = new RepairProposal[proposals.Length];
        int index = 0;
        while (index < proposals.Length)
        {
            coreProposals[index] = SelectCoreProposal(proposals[index]);
            index = index + 1;
        }
        RepairApplicationResult application = new RepairEngine()
            .ApplyRepairs(document, coreProposals);
        SynchronizeProposalStates(proposals);
        return ConvertRepairApplication(application);
    }

    internal OdfRepairAnalysis ConvertRepairAnalysis(
        RepairAnalysisResult analysis)
    {
        OdfRepairAnalysis result = new OdfRepairAnalysis();
        result.SafeProposalCount = analysis.SafeProposalCount;
        result.UnsafeProposalCount = analysis.UnsafeProposalCount;
        result.Proposals = new OdfRepairProposal[analysis.Proposals.Length];
        int index = 0;
        while (index < analysis.Proposals.Length)
        {
            result.Proposals[index] = ConvertRepairProposal(
                analysis.Proposals[index]);
            index = index + 1;
        }
        return result;
    }

    internal OdfRepairProposal ConvertRepairProposal(
        RepairProposal proposal)
    {
        OdfRepairProposal result = new OdfRepairProposal();
        result.Id = proposal.Id;
        result.Kind = ConvertRepairKind(proposal.Kind);
        result.Description = proposal.Description;
        result.Location = OdfDiagnosticFactory.CopyLocation(
            proposal.Location);
        result.IsSafe = proposal.IsSafe;
        result.IsApplied = proposal.IsApplied;
        result.BeforeValue = proposal.OldValue;
        result.AfterValue = proposal.NewValue;
        result.CoreProposal = proposal;
        return result;
    }

    internal RepairProposal SelectCoreProposal(OdfRepairProposal proposal)
    {
        if (proposal == null || proposal.CoreProposal == null)
        {
            return new RepairProposal();
        }
        return proposal.CoreProposal;
    }

    internal void SynchronizeProposalStates(OdfRepairProposal[] proposals)
    {
        int index = 0;
        while (index < proposals.Length)
        {
            OdfRepairProposal proposal = proposals[index];
            if (proposal != null && proposal.CoreProposal != null)
            {
                proposal.IsApplied = proposal.CoreProposal.IsApplied;
            }
            index = index + 1;
        }
    }

    internal OdfRepairResult ConvertRepairApplication(
        RepairApplicationResult application)
    {
        OdfRepairResult result = new OdfRepairResult();
        result.Status = application.Status;
        result.Changed = application.Changed;
        result.AppliedProposalCount = application.AppliedProposalCount;
        result.SkippedProposalCount = application.SkippedProposalCount;
        result.Diagnostics = application.Diagnostics;
        result.Mutations = new OdfRepairMutation[application.Mutations.Length];
        int index = 0;
        while (index < application.Mutations.Length)
        {
            result.Mutations[index] = ConvertRepairMutation(
                application.Mutations[index]);
            index = index + 1;
        }
        return result;
    }

    internal OdfRepairMutation ConvertRepairMutation(
        RepairMutation mutation)
    {
        OdfRepairMutation result = new OdfRepairMutation();
        result.ProposalId = mutation.ProposalId;
        result.Kind = ConvertRepairKind(mutation.Kind);
        result.Description = mutation.Description;
        result.Location = OdfDiagnosticFactory.CopyLocation(
            mutation.Location);
        result.BeforeValue = mutation.BeforeValue;
        result.AfterValue = mutation.AfterValue;
        return result;
    }

    internal OdfRepairKind ConvertRepairKind(RepairKind kind)
    {
        if (kind == RepairKind.PackageMimetype)
        {
            return OdfRepairKind.PackageMimetype;
        }
        if (kind == RepairKind.PackageManifest)
        {
            return OdfRepairKind.PackageManifest;
        }
        if (kind == RepairKind.ManifestSynchronization)
        {
            return OdfRepairKind.ManifestSynchronization;
        }
        if (kind == RepairKind.NamespaceBinding)
        {
            return OdfRepairKind.NamespaceBinding;
        }
        if (kind == RepairKind.XmlIdCache)
        {
            return OdfRepairKind.XmlIdCache;
        }
        if (kind == RepairKind.XmlIdValue)
        {
            return OdfRepairKind.XmlIdValue;
        }
        if (kind == RepairKind.BrokenReference)
        {
            return OdfRepairKind.BrokenReference;
        }
        return OdfRepairKind.Unknown;
    }
}
