namespace uspto;

/// <summary>
/// Selects the independently hydrated sections of a patent dossier.
/// </summary>
public class PatentDossierSelection
{
    /// <summary>Whether to include the complete application wrapper.</summary>
    public bool IncludeApplication = true;

    /// <summary>Whether to include transaction history.</summary>
    public bool IncludeTransactions = false;

    /// <summary>Whether to include file-wrapper document metadata.</summary>
    public bool IncludeDocuments = false;

    /// <summary>Whether to include associated grant and publication metadata.</summary>
    public bool IncludeAssociatedDocuments = false;

    /// <summary>Whether to include domestic continuity relationships.</summary>
    public bool IncludeContinuity = false;

    /// <summary>Whether to include foreign-priority claims.</summary>
    public bool IncludeForeignPriorities = false;

    /// <summary>Whether to include assignment history.</summary>
    public bool IncludeAssignments = false;

    /// <summary>Whether to include attorneys, agents, and correspondence.</summary>
    public bool IncludeAttorneys = false;

    /// <summary>Whether to include patent-term-adjustment data.</summary>
    public bool IncludeTermAdjustment = false;

    /// <summary>Whether to include parsed grant or published-application full text.</summary>
    public bool IncludeFullText = false;

    /// <summary>Whether to include separately labeled PatentsView research records.</summary>
    public bool IncludeResearch = false;
}

/// <summary>
/// Selects a bounded patent portfolio and the dossier sections hydrated for each match.
/// </summary>
public class PatentPortfolioRequest
{
    /// <summary>The attorney surname used to select applications.</summary>
    public string? AttorneyLastName = null;

    /// <summary>The registered practitioner number used to select applications.</summary>
    public string? AttorneyRegistrationNumber = null;

    /// <summary>The correspondence customer number used to select applications.</summary>
    public string? CustomerNumber = null;

    /// <summary>The applicant name text used to select applications.</summary>
    public string? ApplicantName = null;

    /// <summary>The inventor name text used to select applications.</summary>
    public string? InventorName = null;

    /// <summary>The assignee name text used to select applications.</summary>
    public string? AssigneeName = null;

    /// <summary>The application status description used to select applications.</summary>
    public string? Status = null;

    /// <summary>The application status code used to select applications.</summary>
    public int StatusCode = 0;

    /// <summary>Whether <see cref="StatusCode"/> contains a caller-supplied value.</summary>
    public bool HasStatusCode = false;

    /// <summary>The group art unit used to select applications.</summary>
    public string? ArtUnit = null;

    /// <summary>The inclusive lower filing-date bound.</summary>
    public global::System.DateTimeOffset FilingDateFrom = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FilingDateFrom"/> contains a caller-supplied value.</summary>
    public bool HasFilingDateFrom = false;

    /// <summary>The inclusive upper filing-date bound.</summary>
    public global::System.DateTimeOffset FilingDateTo = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FilingDateTo"/> contains a caller-supplied value.</summary>
    public bool HasFilingDateTo = false;

    /// <summary>Whether a dossier is hydrated for every matched application.</summary>
    public bool HydrateDossiers = true;

    /// <summary>The dossier sections hydrated for each matched application.</summary>
    public PatentDossierSelection Selection = new PatentDossierSelection();

    /// <summary>The number of applications requested in each search page.</summary>
    public int PageSize = UsptoDefaults.PageSize;

    /// <summary>The caller limit on portfolio search pages.</summary>
    public int MaximumPages = UsptoDefaults.MaximumPages;

    /// <summary>The caller limit on applications searched and hydrated.</summary>
    public int MaximumResults = UsptoDefaults.MaximumResults;

    /// <summary>The shared context used by the search and every hydration request.</summary>
    public RequestContext Context = new RequestContext();
}

/// <summary>
/// Holds one matched patent application and its optional dossier hydration outcome.
/// </summary>
public class PatentPortfolioEntry
{
    /// <summary>The compact application search result.</summary>
    public PatentApplicationSummary Summary = new PatentApplicationSummary();

    /// <summary>The hydrated dossier, when requested and successfully created.</summary>
    public PatentDossier? Dossier = null;

    /// <summary>The failure that prevented dossier creation.</summary>
    public SectionError? Error = null;

    /// <summary>Whether a dossier was successfully created for this application.</summary>
    public bool IsHydrated = false;
}

/// <summary>
/// Holds a bounded patent portfolio search and its per-application hydration results.
/// </summary>
public class PatentPortfolio
{
    /// <summary>The portfolio request associated with the result.</summary>
    public PatentPortfolioRequest Request = new PatentPortfolioRequest();

    /// <summary>The translated structured search request sent to ODP.</summary>
    public SearchRequest SearchRequest = new SearchRequest();

    /// <summary>The matched applications in deterministic search order.</summary>
    public PatentPortfolioEntry[] Entries = new PatentPortfolioEntry[0];

    /// <summary>The number of matched applications retained in this result.</summary>
    public int Count = 0;

    /// <summary>The number of entries for which a dossier was created.</summary>
    public int HydratedCount = 0;

    /// <summary>The number of entries for which dossier creation failed.</summary>
    public int FailedHydrationCount = 0;

    /// <summary>The service-reported total number of matching applications.</summary>
    public long Total = 0L;

    /// <summary>Whether <see cref="Total"/> was supplied by the service.</summary>
    public bool HasTotal = false;

    /// <summary>The failures that prevented dossier creation or individual nested sections from completing.</summary>
    public SectionError[] Errors = new SectionError[0];

    /// <summary>The number of failed dossier hydrations and nested dossier sections.</summary>
    public int FailedSectionCount = 0;

    /// <summary>Whether the bounded search and every requested dossier completed.</summary>
    public bool IsComplete = false;

    /// <summary>Whether the result retains useful entries alongside an incomplete search or hydration.</summary>
    public bool HasPartialSuccess = false;

    /// <summary>The client-derived provenance of the portfolio composition.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();
}

/// <summary>
/// Reports the outcome and provenance of one requested dossier section.
/// </summary>
public class PatentDossierSectionResult
{
    /// <summary>The section name.</summary>
    public string? Section = null;

    /// <summary>Whether the section contains research-derived rather than official data.</summary>
    public bool IsResearch = false;

    /// <summary>Whether the section loaded successfully.</summary>
    public bool IsComplete = false;

    /// <summary>The section's source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>The captured section failure, when loading failed.</summary>
    public SectionError? Error = null;
}

/// <summary>
/// Holds authoritative USPTO sections without mixing in research-derived fields.
/// </summary>
public class PatentDossierOfficialData
{
    /// <summary>The complete Patent File Wrapper record.</summary>
    public PatentFileWrapper? Application = null;

    /// <summary>The application transaction history.</summary>
    public PatentTransactionHistory? Transactions = null;

    /// <summary>The file-wrapper document listing.</summary>
    public PatentDocumentList? Documents = null;

    /// <summary>The associated grant and publication metadata.</summary>
    public PatentAssociatedDocumentList? AssociatedDocuments = null;

    /// <summary>The domestic continuity relationships.</summary>
    public PatentContinuity? Continuity = null;

    /// <summary>The foreign-priority claims.</summary>
    public PatentForeignPriorityList? ForeignPriorities = null;

    /// <summary>The assignment history.</summary>
    public PatentAssignmentHistory? Assignments = null;

    /// <summary>The attorney, agent, and correspondence record.</summary>
    public PatentAttorneyRecord? Attorneys = null;

    /// <summary>The patent-term-adjustment record.</summary>
    public PatentTermAdjustment? TermAdjustment = null;

    /// <summary>The parsed grant or published-application full text.</summary>
    public PatentFullText? FullText = null;
}

/// <summary>
/// Holds PatentsView research records separately from authoritative USPTO sections.
/// </summary>
public class PatentDossierResearchData
{
    /// <summary>The typed PatentsView patent record.</summary>
    public PatentsViewPatent? PatentRecord = null;

    /// <summary>The typed PatentsView publication record.</summary>
    public PatentsViewPublication? PublicationRecord = null;

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();
}

/// <summary>
/// Holds a selected, partially successful patent dossier with explicit source separation.
/// </summary>
public class PatentDossier
{
    /// <summary>The originally requested patent identifier.</summary>
    public string? RequestedIdentifier = null;

    /// <summary>The resolved canonical application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The selected dossier sections.</summary>
    public PatentDossierSelection Selection = new PatentDossierSelection();

    /// <summary>The authoritative USPTO section values.</summary>
    public PatentDossierOfficialData Official = new PatentDossierOfficialData();

    /// <summary>The separately labeled PatentsView research values.</summary>
    public PatentDossierResearchData Research = new PatentDossierResearchData();

    /// <summary>The result of each requested section in request order.</summary>
    public PatentDossierSectionResult[] Sections = new PatentDossierSectionResult[0];

    /// <summary>The failures captured for individual sections.</summary>
    public SectionError[] Errors = new SectionError[0];

    /// <summary>The number of requested sections.</summary>
    public int RequestedSectionCount = 0;

    /// <summary>The number of successfully hydrated sections.</summary>
    public int CompletedSectionCount = 0;

    /// <summary>The number of failed sections.</summary>
    public int FailedSectionCount = 0;

    /// <summary>Whether every requested section completed successfully.</summary>
    public bool IsComplete = false;

    /// <summary>Whether the dossier contains both successful and failed requested sections.</summary>
    public bool HasPartialSuccess = false;

    /// <summary>The client-derived provenance of the composite result.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();
}
