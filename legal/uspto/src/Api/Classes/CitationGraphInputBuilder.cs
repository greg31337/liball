namespace uspto;

/// <summary>
/// Builds citation graph inputs from normalized Office Action and PatentsView records.
/// </summary>
public class CitationGraphInputBuilder : ICitationGraphInputBuilder
{
    /// <summary>Creates a citation graph input builder.</summary>
    public CitationGraphInputBuilder()
    {
    }

    /// <summary>Builds deterministic citation graph inputs without discarding source-specific evidence.</summary>
    /// <param name="officeActionCitations">The normalized official Office Action citations.</param>
    /// <param name="patentsViewCitations">The research-derived PatentsView citations.</param>
    /// <returns>The paired and unmatched citation graph inputs.</returns>
    public CitationGraphInputSet BuildCitationGraphInputs(OfficeActionCitationResponse officeActionCitations, PatentsViewCitationResponse patentsViewCitations)
    {
        return CitationGraphInputComposer.Build(officeActionCitations, patentsViewCitations);
    }

    /// <summary>Aggregates all available citation evidence and retains independent source failures.</summary>
    /// <param name="request">The available responses and source failures.</param>
    /// <returns>The available citation inputs and structured source outcomes.</returns>
    public CitationGraphInputSet AggregateCitationGraphInputs(CitationGraphAggregationRequest request)
    {
        return CitationGraphInputComposer.CombineAvailableSources(request);
    }
}
