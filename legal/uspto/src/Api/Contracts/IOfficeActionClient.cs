namespace uspto;

/// <summary>
/// Provides read-only access to Office Action text, citation, rejection, and enriched citation datasets.
/// </summary>
public interface IOfficeActionClient
{
    /// <summary>Gets searchable field metadata for an Office Action dataset.</summary>
    /// <param name="datasetName">The stable Office Action dataset name.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed dataset and searchable-field metadata.</returns>
    OfficeActionFieldResponse DiscoverOfficeActionFields(string datasetName, RequestContext context);

    /// <summary>Searches one page of Office Action text.</summary>
    /// <param name="request">The search request.</param>
    /// <returns>Metadata for the returned page.</returns>
    OfficeActionTextResponse SearchOfficeActions(SearchRequest request);

    /// <summary>Searches bounded Office Action text pages.</summary>
    /// <param name="request">The bounded search request.</param>
    /// <returns>Metadata for the aggregated response.</returns>
    OfficeActionTextResponse GetAllOfficeActions(SearchRequest request);

    /// <summary>Searches one page of Office Action citations.</summary>
    /// <param name="request">The search request.</param>
    /// <returns>Metadata for the returned page.</returns>
    OfficeActionCitationResponse SearchOfficeActionCitations(SearchRequest request);

    /// <summary>Searches bounded Office Action citation pages.</summary>
    /// <param name="request">The bounded search request.</param>
    /// <returns>Metadata for the aggregated response.</returns>
    OfficeActionCitationResponse GetAllOfficeActionCitations(SearchRequest request);

    /// <summary>Searches one page of Office Action rejections.</summary>
    /// <param name="request">The search request.</param>
    /// <returns>Metadata for the returned page.</returns>
    OfficeActionRejectionResponse SearchOfficeActionRejections(SearchRequest request);

    /// <summary>Searches bounded Office Action rejection pages.</summary>
    /// <param name="request">The bounded search request.</param>
    /// <returns>Metadata for the aggregated response.</returns>
    OfficeActionRejectionResponse GetAllOfficeActionRejections(SearchRequest request);

    /// <summary>Searches one page of enriched Office Action citations.</summary>
    /// <param name="request">The search request.</param>
    /// <returns>Metadata for the returned page.</returns>
    OfficeActionEnrichedCitationResponse SearchOfficeActionEnrichedCitations(SearchRequest request);

    /// <summary>Searches bounded enriched Office Action citation pages.</summary>
    /// <param name="request">The bounded search request.</param>
    /// <returns>Metadata for the aggregated response.</returns>
    OfficeActionEnrichedCitationResponse GetAllOfficeActionEnrichedCitations(SearchRequest request);

    /// <summary>Gets a prosecution timeline derived from Office Action and file-wrapper records.</summary>
    /// <param name="applicationIdentifier">The application identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed client-derived prosecution timeline.</returns>
    ProsecutionTimeline GetProsecutionTimeline(string applicationIdentifier, RequestContext context);
}
