namespace uspto;

/// <summary>
/// Provides read-only access to final petition decisions and their documents.
/// </summary>
public interface IPetitionClient
{
    /// <summary>Searches one page of final petition decisions.</summary>
    /// <param name="request">The search request.</param>
    /// <returns>The typed petition decisions and facets for the returned page.</returns>
    PetitionDecisionResponse SearchPetitionDecisions(SearchRequest request);

    /// <summary>Searches bounded final petition decision pages.</summary>
    /// <param name="request">The bounded search request.</param>
    /// <returns>The bounded aggregate of typed petition decisions.</returns>
    PetitionDecisionResponse GetAllPetitionDecisions(SearchRequest request);

    /// <summary>Downloads a petition decision search export.</summary>
    /// <param name="search">The search and representation to export.</param>
    /// <param name="download">The download request.</param>
    /// <returns>The completed or failed download result.</returns>
    DownloadResult DownloadPetitionDecisionSearch(PetitionDecisionExportRequest search, DownloadRequest download);

    /// <summary>Gets one final petition decision.</summary>
    /// <param name="recordIdentifier">The decision record identifier.</param>
    /// <param name="includeDocuments">Whether associated decision documents are requested.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed final petition decision.</returns>
    PetitionDecision GetPetitionDecision(string recordIdentifier, bool includeDocuments, RequestContext context);

    /// <summary>Downloads one petition decision document.</summary>
    /// <param name="documentIdentifier">The service document identifier.</param>
    /// <param name="request">The download request.</param>
    /// <returns>The completed or failed download result.</returns>
    DownloadResult DownloadPetitionDocument(string documentIdentifier, DownloadRequest request);
}
