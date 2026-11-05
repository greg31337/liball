namespace uspto;

/// <summary>
/// Combines normalized official and research citation records into graph inputs.
/// </summary>
public interface ICitationGraphInputBuilder
{
    /// <summary>Builds deterministic citation graph inputs without discarding source-specific evidence.</summary>
    /// <param name="officeActionCitations">The normalized official Office Action citations.</param>
    /// <param name="patentsViewCitations">The research-derived PatentsView citations.</param>
    /// <returns>The paired and unmatched citation graph inputs.</returns>
    CitationGraphInputSet BuildCitationGraphInputs(OfficeActionCitationResponse officeActionCitations, PatentsViewCitationResponse patentsViewCitations);

    /// <summary>Aggregates all available citation evidence and retains independent source failures.</summary>
    /// <param name="request">The available responses and source failures.</param>
    /// <returns>The available citation inputs and structured source outcomes.</returns>
    CitationGraphInputSet AggregateCitationGraphInputs(CitationGraphAggregationRequest request);
}
