namespace uspto;

/// <summary>
/// Describes one representation available for downloading a petition decision document.
/// </summary>
public class PetitionDocumentDownloadAlternative
{
    /// <summary>The source MIME type or format identifier.</summary>
    public string? MimeTypeIdentifier = null;

    /// <summary>The normalized MIME media type, when recognized.</summary>
    public string? MediaType = null;

    /// <summary>The recognized document representation.</summary>
    public DocumentFormat Format = DocumentFormat.Unknown;

    /// <summary>The credential-free URI for this representation.</summary>
    public global::System.Uri? DownloadUri = null;

    /// <summary>The number of pages in this representation.</summary>
    public int PageCount = 0;

    /// <summary>Whether <see cref="PageCount"/> was supplied.</summary>
    public bool HasPageCount = false;

    /// <summary>Unmodeled download-option fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Describes one document associated with a final petition decision.
/// </summary>
public class PetitionDecisionDocument
{
    /// <summary>The patent application number associated with the document.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The source identifier assigned to the document.</summary>
    public string? DocumentIdentifier = null;

    /// <summary>The USPTO document code.</summary>
    public string? Code = null;

    /// <summary>The human-readable document-code description.</summary>
    public string? Description = null;

    /// <summary>The incoming, outgoing, or internal document direction.</summary>
    public string? DirectionCategory = null;

    /// <summary>The official date assigned to the document.</summary>
    public global::System.DateTimeOffset OfficialDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="OfficialDate"/> was supplied.</summary>
    public bool HasOfficialDate = false;

    /// <summary>The representations available for downloading the document.</summary>
    public PetitionDocumentDownloadAlternative[] DownloadAlternatives = new PetitionDocumentDownloadAlternative[0];

    /// <summary>Unmodeled document fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one final petition decision and its optionally included documents.
/// </summary>
public class PetitionDecision
{
    /// <summary>The stable petition decision record identifier.</summary>
    public string? RecordIdentifier = null;

    /// <summary>The patent application number associated with the petition.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The patent number associated with the petition.</summary>
    public string? PatentNumber = null;

    /// <summary>The first applicant name reported by the source.</summary>
    public string? FirstApplicantName = null;

    /// <summary>The invention title.</summary>
    public string? InventionTitle = null;

    /// <summary>The correspondence customer number retained as identifier text.</summary>
    public string? CustomerNumber = null;

    /// <summary>The applicant business-entity status category.</summary>
    public string? BusinessEntityStatusCategory = null;

    /// <summary>The petition decision date.</summary>
    public global::System.DateTimeOffset DecisionDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="DecisionDate"/> was supplied.</summary>
    public bool HasDecisionDate = false;

    /// <summary>The decision mailing date when reported separately.</summary>
    public global::System.DateTimeOffset DecisionMailDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="DecisionMailDate"/> was supplied.</summary>
    public bool HasDecisionMailDate = false;

    /// <summary>The petition mailing date.</summary>
    public global::System.DateTimeOffset PetitionMailDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="PetitionMailDate"/> was supplied.</summary>
    public bool HasPetitionMailDate = false;

    /// <summary>The numeric petition-type code.</summary>
    public int PetitionTypeCode = 0;

    /// <summary>Whether <see cref="PetitionTypeCode"/> was supplied.</summary>
    public bool HasPetitionTypeCode = false;

    /// <summary>The decision outcome or category code.</summary>
    public string? DecisionTypeCode = null;

    /// <summary>The human-readable petition-type description.</summary>
    public string? PetitionTypeDescription = null;

    /// <summary>The human-readable decision outcome or category description.</summary>
    public string? DecisionTypeDescription = null;

    /// <summary>The office responsible for the final decision.</summary>
    public string? FinalDecidingOfficeName = null;

    /// <summary>Whether the application follows first-inventor-to-file rules.</summary>
    public bool IsFirstInventorToFile = false;

    /// <summary>Whether <see cref="IsFirstInventorToFile"/> was supplied.</summary>
    public bool HasIsFirstInventorToFile = false;

    /// <summary>The group art unit associated with the application.</summary>
    public string? ArtUnit = null;

    /// <summary>The technology center associated with the application.</summary>
    public string? TechnologyCenter = null;

    /// <summary>The inventor names reported for the petition.</summary>
    public string[] Inventors = new string[0];

    /// <summary>The court action taken on the petition.</summary>
    public string? CourtActionName = null;

    /// <summary>Whether the petition record reports a court action.</summary>
    public bool CourtActionIndicator = false;

    /// <summary>Whether <see cref="CourtActionIndicator"/> contains a source-supplied value.</summary>
    public bool HasCourtActionIndicator = false;

    /// <summary>The issues considered in the petition decision.</summary>
    public string[] IssuesConsidered = new string[0];

    /// <summary>The current prosecution-status description.</summary>
    public string? ProsecutionStatusDescription = null;

    /// <summary>The cited regulations.</summary>
    public string[] Rules = new string[0];

    /// <summary>The cited statutes.</summary>
    public string[] Statutes = new string[0];

    /// <summary>The source ingestion time.</summary>
    public global::System.DateTimeOffset LastIngestedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastIngestedAt"/> was supplied.</summary>
    public bool HasLastIngestedAt = false;

    /// <summary>The petition decision documents included by direct retrieval.</summary>
    public PetitionDecisionDocument[] Documents = new PetitionDecisionDocument[0];

    /// <summary>The origin and authority of this petition decision.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>Unmodeled petition decision fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one value and count returned for a petition decision facet.
/// </summary>
public class PetitionDecisionFacetValue
{
    /// <summary>The facet value.</summary>
    public string? Value = null;

    /// <summary>The number of matching petition decisions.</summary>
    public long Count = 0L;
}

/// <summary>
/// Holds all returned values for one petition decision facet field.
/// </summary>
public class PetitionDecisionFacet
{
    /// <summary>The petition decision field that was aggregated.</summary>
    public string? Field = null;

    /// <summary>The returned facet values and counts.</summary>
    public PetitionDecisionFacetValue[] Values = new PetitionDecisionFacetValue[0];
}

/// <summary>
/// Holds one page or bounded aggregate of final petition decisions.
/// </summary>
public class PetitionDecisionResponse
{
    /// <summary>The search request associated with the response.</summary>
    public SearchRequest Request = new SearchRequest();

    /// <summary>The returned final petition decisions.</summary>
    public PetitionDecision[] Decisions = new PetitionDecision[0];

    /// <summary>The returned facet data.</summary>
    public PetitionDecisionFacet[] Facets = new PetitionDecisionFacet[0];

    /// <summary>The number of decisions held in this response.</summary>
    public int Count = 0;

    /// <summary>The service-reported total number of matching decisions.</summary>
    public long Total = 0L;

    /// <summary>Whether <see cref="Total"/> was supplied.</summary>
    public bool HasTotal = false;

    /// <summary>The offset-page coordinates associated with the response.</summary>
    public OffsetPage Page = new OffsetPage();

    /// <summary>The service request identifier.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and authority of the response.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>A bounded retained raw response, when requested.</summary>
    public string? RawResponse = null;

    /// <summary>Whether all requested pages were processed.</summary>
    public bool IsComplete = false;
}

/// <summary>
/// Selects a petition decision search export and its representation.
/// </summary>
public class PetitionDecisionExportRequest
{
    /// <summary>The petition decision search to export.</summary>
    public SearchRequest Search = new SearchRequest();

    /// <summary>The requested JSON or CSV export representation.</summary>
    public ExportFormat Format = ExportFormat.Json;
}
