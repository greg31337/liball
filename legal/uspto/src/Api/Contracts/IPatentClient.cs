namespace uspto;

/// <summary>
/// Provides read-only access to Patent File Wrapper and patent application operations.
/// </summary>
public interface IPatentClient
{
    /// <summary>Searches one page of patent applications.</summary>
    /// <param name="request">The search request.</param>
    /// <returns>Metadata for the returned page.</returns>
    PatentApplicationResponse SearchPatentApplications(SearchRequest request);

    /// <summary>Searches bounded patent application pages.</summary>
    /// <param name="request">The bounded search request.</param>
    /// <returns>Metadata for the aggregated response.</returns>
    PatentApplicationResponse GetAllPatentApplications(SearchRequest request);

    /// <summary>Downloads a patent application search export.</summary>
    /// <param name="search">The search to export.</param>
    /// <param name="download">The download destination and verification request.</param>
    /// <returns>The completed or failed download result.</returns>
    DownloadResult DownloadPatentApplicationSearch(SearchRequest search, DownloadRequest download);

    /// <summary>Resolves a grant, publication, or PCT identifier to an application identifier.</summary>
    /// <param name="identifier">The source identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The resolved canonical application identifier.</returns>
    string ResolvePatentApplicationIdentifier(string identifier, RequestContext context);

    /// <summary>Gets a complete patent application wrapper.</summary>
    /// <param name="identifier">The application or resolvable patent identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The complete modeled Patent File Wrapper record.</returns>
    PatentFileWrapper GetPatentApplication(string identifier, RequestContext context);

    /// <summary>Gets patent application metadata.</summary>
    /// <param name="identifier">The application identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The modeled patent application metadata.</returns>
    PatentApplicationMetadata GetPatentMetadata(string identifier, RequestContext context);

    /// <summary>Gets patent application transaction history.</summary>
    /// <param name="identifier">The application identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed transaction history in source order.</returns>
    PatentTransactionHistory GetPatentTransactions(string identifier, RequestContext context);

    /// <summary>Gets patent application document metadata.</summary>
    /// <param name="identifier">The application identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed document listing and download alternatives.</returns>
    PatentDocumentList GetPatentDocuments(string identifier, RequestContext context);

    /// <summary>Downloads one Patent File Wrapper document.</summary>
    /// <param name="documentIdentifier">The service document identifier.</param>
    /// <param name="request">The download request.</param>
    /// <returns>The completed or failed download result.</returns>
    DownloadResult DownloadPatentDocument(string documentIdentifier, DownloadRequest request);

    /// <summary>Gets associated publication and grant document metadata.</summary>
    /// <param name="identifier">The application identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed grant and pre-grant publication file metadata.</returns>
    PatentAssociatedDocumentList GetPatentAssociatedDocuments(string identifier, RequestContext context);

    /// <summary>Gets domestic continuity relationships.</summary>
    /// <param name="identifier">The application identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed parent and child continuity relationships.</returns>
    PatentContinuity GetPatentContinuity(string identifier, RequestContext context);

    /// <summary>Gets foreign priority claims.</summary>
    /// <param name="identifier">The application identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed foreign priority claims in source order.</returns>
    PatentForeignPriorityList GetPatentForeignPriorities(string identifier, RequestContext context);

    /// <summary>Gets recorded patent assignments.</summary>
    /// <param name="identifier">The application identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed assignment history in source order.</returns>
    PatentAssignmentHistory GetPatentAssignments(string identifier, RequestContext context);

    /// <summary>Gets attorney and agent data.</summary>
    /// <param name="identifier">The application identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed attorney, agent, and correspondence record.</returns>
    PatentAttorneyRecord GetPatentAttorneys(string identifier, RequestContext context);

    /// <summary>Gets patent term adjustment data.</summary>
    /// <param name="identifier">The application identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed adjustment totals and calculation history.</returns>
    PatentTermAdjustment GetPatentTermAdjustment(string identifier, RequestContext context);

    /// <summary>Searches the patent application status-code catalog.</summary>
    /// <param name="request">The search request.</param>
    /// <returns>The typed status-code page and description lookup.</returns>
    PatentStatusCodeResponse SearchPatentStatusCodes(SearchRequest request);

    /// <summary>Searches bounded pages of the patent application status-code catalog.</summary>
    /// <param name="request">The bounded search request.</param>
    /// <returns>The aggregated status codes and description lookup.</returns>
    PatentStatusCodeResponse GetAllPatentStatusCodes(SearchRequest request);

    /// <summary>Gets and parses associated patent full text.</summary>
    /// <param name="identifier">The application, publication, or grant identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed grant or published-application full-text record.</returns>
    PatentFullText GetPatentFullText(string identifier, RequestContext context);

    /// <summary>Gets a selected-section patent dossier.</summary>
    /// <param name="identifier">The application or patent identifier.</param>
    /// <param name="selection">The sections to hydrate.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed composite dossier with per-section outcomes.</returns>
    PatentDossier GetPatentDossier(string identifier, PatentDossierSelection selection, RequestContext context);

    /// <summary>Gets a bounded patent portfolio.</summary>
    /// <param name="request">The portfolio search request.</param>
    /// <returns>The bounded matches and their selected dossier sections.</returns>
    PatentPortfolio GetPatentPortfolio(PatentPortfolioRequest request);
}
