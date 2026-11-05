namespace uspto;

/// <summary>
/// Provides read-only access to PTAB proceedings, decisions, documents, appeals, and interferences.
/// </summary>
public interface IPtabClient
{
    /// <summary>Searches one page of PTAB trial proceedings.</summary>
    /// <param name="request">The search request.</param>
    /// <returns>The typed proceedings and facets for the returned page.</returns>
    PtabTrialProceedingResponse SearchPtabProceedings(SearchRequest request);

    /// <summary>Searches bounded PTAB trial proceeding pages.</summary>
    /// <param name="request">The bounded search request.</param>
    /// <returns>The bounded aggregate of typed proceedings.</returns>
    PtabTrialProceedingResponse GetAllPtabProceedings(SearchRequest request);

    /// <summary>Downloads a PTAB proceeding search export.</summary>
    /// <param name="search">The search to export.</param>
    /// <param name="download">The download request.</param>
    /// <returns>The download result.</returns>
    DownloadResult DownloadPtabProceedingSearch(SearchRequest search, DownloadRequest download);

    /// <summary>Gets one PTAB trial proceeding.</summary>
    /// <param name="trialNumber">The PTAB trial number.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed PTAB trial proceeding.</returns>
    PtabTrialProceeding GetPtabProceeding(string trialNumber, RequestContext context);

    /// <summary>Searches one page of PTAB trial decisions.</summary>
    /// <param name="request">The search request.</param>
    /// <returns>The typed trial decisions and facets for the returned page.</returns>
    PtabTrialDecisionResponse SearchPtabTrialDecisions(SearchRequest request);

    /// <summary>Searches bounded PTAB trial decision pages.</summary>
    /// <param name="request">The bounded search request.</param>
    /// <returns>The bounded aggregate of typed trial decisions.</returns>
    PtabTrialDecisionResponse GetAllPtabTrialDecisions(SearchRequest request);

    /// <summary>Downloads a PTAB trial decision search export.</summary>
    /// <param name="search">The search to export.</param>
    /// <param name="download">The download request.</param>
    /// <returns>The download result.</returns>
    DownloadResult DownloadPtabTrialDecisionSearch(SearchRequest search, DownloadRequest download);

    /// <summary>Gets one PTAB trial decision.</summary>
    /// <param name="documentIdentifier">The decision document identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed PTAB trial decision.</returns>
    PtabTrialDecision GetPtabTrialDecision(string documentIdentifier, RequestContext context);

    /// <summary>Gets PTAB decisions filed in one trial.</summary>
    /// <param name="trialNumber">The PTAB trial number.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed PTAB trial decisions.</returns>
    PtabTrialDecisionResponse GetPtabTrialDecisionsByTrial(string trialNumber, RequestContext context);

    /// <summary>Searches one page of PTAB trial documents.</summary>
    /// <param name="request">The search request.</param>
    /// <returns>The typed trial documents and facets for the returned page.</returns>
    PtabTrialDocumentResponse SearchPtabTrialDocuments(SearchRequest request);

    /// <summary>Searches bounded PTAB trial document pages.</summary>
    /// <param name="request">The bounded search request.</param>
    /// <returns>The bounded aggregate of typed trial documents.</returns>
    PtabTrialDocumentResponse GetAllPtabTrialDocuments(SearchRequest request);

    /// <summary>Downloads a PTAB trial document search export.</summary>
    /// <param name="search">The search to export.</param>
    /// <param name="download">The download request.</param>
    /// <returns>The download result.</returns>
    DownloadResult DownloadPtabTrialDocumentSearch(SearchRequest search, DownloadRequest download);

    /// <summary>Gets one PTAB trial document record.</summary>
    /// <param name="documentIdentifier">The service document identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed PTAB trial document.</returns>
    PtabTrialDocument GetPtabTrialDocument(string documentIdentifier, RequestContext context);

    /// <summary>Gets PTAB documents filed in one trial.</summary>
    /// <param name="trialNumber">The PTAB trial number.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed PTAB trial documents.</returns>
    PtabTrialDocumentResponse GetPtabTrialDocumentsByTrial(string trialNumber, RequestContext context);

    /// <summary>Searches one page of PTAB appeal decisions.</summary>
    /// <param name="request">The search request.</param>
    /// <returns>The typed appeal decisions and facets for the returned page.</returns>
    PtabAppealDecisionResponse SearchPtabAppealDecisions(SearchRequest request);

    /// <summary>Searches bounded PTAB appeal decision pages.</summary>
    /// <param name="request">The bounded search request.</param>
    /// <returns>The bounded aggregate of typed appeal decisions.</returns>
    PtabAppealDecisionResponse GetAllPtabAppealDecisions(SearchRequest request);

    /// <summary>Downloads a PTAB appeal decision search export.</summary>
    /// <param name="search">The search to export.</param>
    /// <param name="download">The download request.</param>
    /// <returns>The download result.</returns>
    DownloadResult DownloadPtabAppealDecisionSearch(SearchRequest search, DownloadRequest download);

    /// <summary>Gets one PTAB appeal decision.</summary>
    /// <param name="documentIdentifier">The decision document identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed PTAB appeal decision.</returns>
    PtabAppealDecision GetPtabAppealDecision(string documentIdentifier, RequestContext context);

    /// <summary>Gets PTAB decisions associated with one appeal.</summary>
    /// <param name="appealNumber">The PTAB appeal number.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed PTAB appeal decisions.</returns>
    PtabAppealDecisionResponse GetPtabAppealDecisionsByAppeal(string appealNumber, RequestContext context);

    /// <summary>Searches one page of PTAB interference decisions.</summary>
    /// <param name="request">The search request.</param>
    /// <returns>The typed interference decisions and facets for the returned page.</returns>
    PtabInterferenceDecisionResponse SearchPtabInterferenceDecisions(SearchRequest request);

    /// <summary>Searches bounded PTAB interference decision pages.</summary>
    /// <param name="request">The bounded search request.</param>
    /// <returns>The bounded aggregate of typed interference decisions.</returns>
    PtabInterferenceDecisionResponse GetAllPtabInterferenceDecisions(SearchRequest request);

    /// <summary>Downloads a PTAB interference decision search export.</summary>
    /// <param name="search">The search to export.</param>
    /// <param name="download">The download request.</param>
    /// <returns>The download result.</returns>
    DownloadResult DownloadPtabInterferenceDecisionSearch(SearchRequest search, DownloadRequest download);

    /// <summary>Gets one PTAB interference decision.</summary>
    /// <param name="documentIdentifier">The decision document identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed PTAB interference decision.</returns>
    PtabInterferenceDecision GetPtabInterferenceDecision(string documentIdentifier, RequestContext context);

    /// <summary>Gets PTAB decisions associated with one interference.</summary>
    /// <param name="interferenceNumber">The PTAB interference number.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed PTAB interference decisions.</returns>
    PtabInterferenceDecisionResponse GetPtabInterferenceDecisionsByInterference(string interferenceNumber, RequestContext context);

    /// <summary>Downloads a document exposed by a PTAB collection.</summary>
    /// <param name="collection">The PTAB collection containing the document.</param>
    /// <param name="documentIdentifier">The service document identifier.</param>
    /// <param name="request">The download request.</param>
    /// <returns>The download result.</returns>
    DownloadResult DownloadPtabDocument(PtabCollection collection, string documentIdentifier, DownloadRequest request);

    /// <summary>Gets a combined PTAB proceeding dossier.</summary>
    /// <param name="trialNumber">The PTAB trial number.</param>
    /// <param name="selection">The sections and bounded downloads to include.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed composite dossier with per-section outcomes.</returns>
    PtabDossier GetPtabDossier(string trialNumber, PtabDossierSelection selection, RequestContext context);
}
