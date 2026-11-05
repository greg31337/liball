namespace uspto;

/// <summary>
/// Selects the sections and bounded document content included in a trademark dossier.
/// </summary>
public class TrademarkDossierSelection
{
    /// <summary>Whether to include parsed trademark case status and its derived case sections.</summary>
    public bool IncludeStatus = true;

    /// <summary>The JSON or XML representation used to retrieve parsed case status.</summary>
    public DocumentFormat StatusFormat = DocumentFormat.Json;

    /// <summary>Whether to include the trademark case document listing.</summary>
    public bool IncludeDocuments = true;

    /// <summary>The explicitly selected document content transfers.</summary>
    public TrademarkDossierContentRequest[] SelectedContent = new TrademarkDossierContentRequest[0];

    /// <summary>The maximum number of selected content transfers accepted by this request.</summary>
    public int MaximumSelectedContent = 20;
}

/// <summary>
/// Selects one trademark case document representation for inclusion in a dossier.
/// </summary>
public class TrademarkDossierContentRequest
{
    /// <summary>The TSDR document identifier.</summary>
    public string? DocumentIdentifier = null;

    /// <summary>The requested PDF or ZIP representation.</summary>
    public DocumentFormat Format = DocumentFormat.Pdf;

    /// <summary>The stream-backed or file-backed content request.</summary>
    public DownloadRequest Download = new DownloadRequest();
}

/// <summary>
/// Groups the owners, attorneys, and correspondence associated with a trademark case.
/// </summary>
public class TrademarkDossierParties
{
    /// <summary>The owners currently associated with the case.</summary>
    public TrademarkOwner[] CurrentOwners = new TrademarkOwner[0];

    /// <summary>The owners formerly associated with the case.</summary>
    public TrademarkOwner[] PriorOwners = new TrademarkOwner[0];

    /// <summary>The attorneys and representatives associated with the case.</summary>
    public TrademarkAttorney[] Attorneys = new TrademarkAttorney[0];

    /// <summary>The current correspondence contact and address.</summary>
    public TrademarkCorrespondence Correspondence = new TrademarkCorrespondence();
}

/// <summary>
/// Reports one selected trademark document content transfer.
/// </summary>
public class TrademarkDossierContentResult
{
    /// <summary>The requested TSDR document identifier.</summary>
    public string? DocumentIdentifier = null;

    /// <summary>The requested document representation.</summary>
    public DocumentFormat Format = DocumentFormat.Unknown;

    /// <summary>The matching document metadata, when the document listing was available.</summary>
    public TrademarkDocument? Document = null;

    /// <summary>The completed or terminal content result, when a transfer was attempted.</summary>
    public DownloadResult? Download = null;

    /// <summary>The captured lookup or transfer failure.</summary>
    public SectionError? Error = null;

    /// <summary>Whether the selected content transfer completed successfully.</summary>
    public bool IsComplete = false;
}

/// <summary>
/// Reports the outcome and provenance of one requested trademark dossier section.
/// </summary>
public class TrademarkDossierSectionResult
{
    /// <summary>The stable section name.</summary>
    public string? Section = null;

    /// <summary>Whether the section completed successfully.</summary>
    public bool IsComplete = false;

    /// <summary>The section source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>The captured section failure.</summary>
    public SectionError? Error = null;
}

/// <summary>
/// Holds a composite trademark dossier with independently reported section outcomes.
/// </summary>
public class TrademarkDossier
{
    /// <summary>The trademark identifier supplied by the caller.</summary>
    public string? RequestedIdentifier = null;

    /// <summary>The canonical trademark identifier used for TSDR requests.</summary>
    public string? CanonicalIdentifier = null;

    /// <summary>The kind of canonical trademark identifier.</summary>
    public TrademarkIdentifierKind IdentifierKind = TrademarkIdentifierKind.Unknown;

    /// <summary>The selected sections and document content.</summary>
    public TrademarkDossierSelection Selection = new TrademarkDossierSelection();

    /// <summary>The complete parsed case-status record, when requested and available.</summary>
    public TrademarkCaseStatus? CaseStatus = null;

    /// <summary>The current lifecycle status, when case status was available.</summary>
    public TrademarkStatus? Status = null;

    /// <summary>The mark representation, when case status was available.</summary>
    public TrademarkMark? Mark = null;

    /// <summary>The prosecution-history events in source order.</summary>
    public TrademarkProsecutionEvent[] Events = new TrademarkProsecutionEvent[0];

    /// <summary>The case owners, attorneys, and correspondence, when status was available.</summary>
    public TrademarkDossierParties? Parties = null;

    /// <summary>The goods-and-services entries and classifications in source order.</summary>
    public TrademarkGoodsAndService[] GoodsAndServices = new TrademarkGoodsAndService[0];

    /// <summary>The complete document listing response, when requested and available.</summary>
    public TrademarkDocumentCollection? DocumentCollection = null;

    /// <summary>The case documents in source order.</summary>
    public TrademarkDocument[] Documents = new TrademarkDocument[0];

    /// <summary>The selected document content outcomes in request order.</summary>
    public TrademarkDossierContentResult[] SelectedContent = new TrademarkDossierContentResult[0];

    /// <summary>The result of each requested section in request order.</summary>
    public TrademarkDossierSectionResult[] Sections = new TrademarkDossierSectionResult[0];

    /// <summary>The failures captured for individual sections or content transfers.</summary>
    public SectionError[] Errors = new SectionError[0];

    /// <summary>The number of requested sections and content transfers.</summary>
    public int RequestedSectionCount = 0;

    /// <summary>The number of successfully completed sections and content transfers.</summary>
    public int CompletedSectionCount = 0;

    /// <summary>The number of failed sections and content transfers.</summary>
    public int FailedSectionCount = 0;

    /// <summary>Whether every requested section and content transfer completed successfully.</summary>
    public bool IsComplete = false;

    /// <summary>Whether successful data is retained alongside one or more failures.</summary>
    public bool HasPartialSuccess = false;

    /// <summary>The client-derived provenance of the composite dossier.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();
}
