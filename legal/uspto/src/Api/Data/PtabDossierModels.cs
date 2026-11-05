namespace uspto;

/// <summary>
/// Selects PTAB dossier sections and bounded document downloads.
/// </summary>
public class PtabDossierSelection
{
    /// <summary>Whether to include the trial proceeding.</summary>
    public bool IncludeProceeding = true;

    /// <summary>Whether to include decisions associated with the trial.</summary>
    public bool IncludeDecisions = true;

    /// <summary>Whether to include documents associated with the trial.</summary>
    public bool IncludeDocuments = true;

    /// <summary>The explicitly requested document downloads.</summary>
    public PtabDossierDownloadRequest[] Downloads = new PtabDossierDownloadRequest[0];

    /// <summary>The maximum number of document downloads accepted by this request.</summary>
    public int MaximumDownloads = 20;
}

/// <summary>
/// Selects one document in a PTAB dossier for download.
/// </summary>
public class PtabDossierDownloadRequest
{
    /// <summary>The service document identifier to download.</summary>
    public string? DocumentIdentifier = null;

    /// <summary>The stream-backed or file-backed download request.</summary>
    public DownloadRequest Download = new DownloadRequest();
}

/// <summary>
/// Holds one document deduplicated across PTAB decision and document collections.
/// </summary>
public class PtabDossierDocument
{
    /// <summary>The preferred document metadata.</summary>
    public PtabDocument Document = new PtabDocument();

    /// <summary>The corresponding decision record, when present.</summary>
    public PtabTrialDecision? DecisionRecord = null;

    /// <summary>The corresponding trial-document record, when present.</summary>
    public PtabTrialDocument? TrialDocumentRecord = null;

    /// <summary>Whether the document appeared in the trial-decision collection.</summary>
    public bool InDecisionCollection = false;

    /// <summary>Whether the document appeared in the trial-document collection.</summary>
    public bool InDocumentCollection = false;
}

/// <summary>
/// Reports one requested PTAB dossier document download.
/// </summary>
public class PtabDossierDownloadResult
{
    /// <summary>The requested service document identifier.</summary>
    public string? DocumentIdentifier = null;

    /// <summary>The collection selected for the download.</summary>
    public PtabCollection Collection = PtabCollection.TrialDocuments;

    /// <summary>The completed or terminal download result, when a transfer was attempted.</summary>
    public DownloadResult? Download = null;

    /// <summary>The captured lookup or transfer failure.</summary>
    public SectionError? Error = null;

    /// <summary>Whether the requested download completed successfully.</summary>
    public bool IsComplete = false;
}

/// <summary>
/// Reports the outcome and provenance of one requested PTAB dossier section.
/// </summary>
public class PtabDossierSectionResult
{
    /// <summary>The stable section name.</summary>
    public string? Section = null;

    /// <summary>Whether the section completed successfully.</summary>
    public bool IsComplete = false;

    /// <summary>The section source and retrieval context.</summary>
    public PtabDataProvenance Provenance = new PtabDataProvenance();

    /// <summary>The captured section failure.</summary>
    public SectionError? Error = null;
}

/// <summary>
/// Holds a composite PTAB trial dossier with independently reported section outcomes.
/// </summary>
public class PtabDossier
{
    /// <summary>The requested PTAB trial number.</summary>
    public string? TrialNumber = null;

    /// <summary>The selected sections and downloads.</summary>
    public PtabDossierSelection Selection = new PtabDossierSelection();

    /// <summary>The trial proceeding, when requested and available.</summary>
    public PtabTrialProceeding? Proceeding = null;

    /// <summary>The trial decisions in service order.</summary>
    public PtabTrialDecision[] Decisions = new PtabTrialDecision[0];

    /// <summary>The trial-document records in service order.</summary>
    public PtabTrialDocument[] TrialDocuments = new PtabTrialDocument[0];

    /// <summary>The documents deduplicated across decision and trial-document records.</summary>
    public PtabDossierDocument[] Documents = new PtabDossierDocument[0];

    /// <summary>The requested document download outcomes.</summary>
    public PtabDossierDownloadResult[] Downloads = new PtabDossierDownloadResult[0];

    /// <summary>The result of each requested section in request order.</summary>
    public PtabDossierSectionResult[] Sections = new PtabDossierSectionResult[0];

    /// <summary>The failures captured for individual sections or downloads.</summary>
    public SectionError[] Errors = new SectionError[0];

    /// <summary>The number of requested sections and downloads.</summary>
    public int RequestedSectionCount = 0;

    /// <summary>The number of successfully completed sections and downloads.</summary>
    public int CompletedSectionCount = 0;

    /// <summary>The number of failed sections and downloads.</summary>
    public int FailedSectionCount = 0;

    /// <summary>Whether every requested section and download completed successfully.</summary>
    public bool IsComplete = false;

    /// <summary>Whether successful data is retained alongside one or more failures.</summary>
    public bool HasPartialSuccess = false;

    /// <summary>The client-derived provenance of the composite dossier.</summary>
    public PtabDataProvenance Provenance = new PtabDataProvenance();
}
