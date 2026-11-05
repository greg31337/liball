namespace uspto;

/// <summary>
/// Describes the applicant entity status associated with a patent application.
/// </summary>
public class PatentEntityStatus
{
    /// <summary>The business entity status category reported by the USPTO.</summary>
    public string? Category = null;

    /// <summary>Whether the applicant qualifies for small-entity status.</summary>
    public bool IsSmallEntity = false;

    /// <summary>Whether <see cref="IsSmallEntity"/> was supplied.</summary>
    public bool HasIsSmallEntity = false;

    /// <summary>Whether the applicant qualifies for micro-entity status.</summary>
    public bool IsMicroEntity = false;

    /// <summary>Whether <see cref="IsMicroEntity"/> was supplied.</summary>
    public bool HasIsMicroEntity = false;
}

/// <summary>
/// Identifies the source, authority, and retrieval context of patent application data.
/// </summary>
public class PatentDataProvenance
{
    /// <summary>The origin and authority category.</summary>
    public DataProvenance Source = DataProvenance.Unknown;

    /// <summary>The credential-free source URI, when available.</summary>
    public global::System.Uri? SourceUri = null;

    /// <summary>The source dataset or product identifier.</summary>
    public string? DatasetIdentifier = null;

    /// <summary>The source dataset version.</summary>
    public string? DatasetVersion = null;

    /// <summary>The service request identifier associated with the data.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The time at which the data was retrieved.</summary>
    public global::System.DateTimeOffset RetrievedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="RetrievedAt"/> was supplied.</summary>
    public bool HasRetrievedAt = false;

    /// <summary>The source's reported last-update time.</summary>
    public global::System.DateTimeOffset LastUpdatedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastUpdatedAt"/> was supplied.</summary>
    public bool HasLastUpdatedAt = false;

    /// <summary>Whether the record came from a response cache.</summary>
    public bool FromCache = false;

    /// <summary>Whether the source is authoritative for USPTO application status.</summary>
    public bool IsAuthoritative = false;

    /// <summary>The origin and delivery labels for individual fields in composite, cached, or local results.</summary>
    public FieldDataProvenance[] FieldSources = new FieldDataProvenance[0];
}

/// <summary>
/// Holds the compact application fields returned by patent search operations.
/// </summary>
public class PatentApplicationSummary
{
    /// <summary>The canonical patent application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The patent number associated with the application.</summary>
    public string? PatentNumber = null;

    /// <summary>The earliest or selected pre-grant publication number.</summary>
    public string? PublicationNumber = null;

    /// <summary>The invention title.</summary>
    public string? InventionTitle = null;

    /// <summary>The application type code.</summary>
    public string? ApplicationTypeCode = null;

    /// <summary>The application type label.</summary>
    public string? ApplicationTypeLabel = null;

    /// <summary>The filing date.</summary>
    public global::System.DateTimeOffset FilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FilingDate"/> was supplied.</summary>
    public bool HasFilingDate = false;

    /// <summary>The earliest publication date.</summary>
    public global::System.DateTimeOffset PublicationDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="PublicationDate"/> was supplied.</summary>
    public bool HasPublicationDate = false;

    /// <summary>The grant date.</summary>
    public global::System.DateTimeOffset GrantDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="GrantDate"/> was supplied.</summary>
    public bool HasGrantDate = false;

    /// <summary>The numeric application status code.</summary>
    public int ApplicationStatusCode = 0;

    /// <summary>Whether <see cref="ApplicationStatusCode"/> was supplied.</summary>
    public bool HasApplicationStatusCode = false;

    /// <summary>The application status description.</summary>
    public string? ApplicationStatusDescription = null;

    /// <summary>The first applicant name reported by the source.</summary>
    public string? FirstApplicantName = null;

    /// <summary>The first inventor name reported by the source.</summary>
    public string? FirstInventorName = null;

    /// <summary>The group art unit.</summary>
    public string? ArtUnit = null;

    /// <summary>The origin and authority of this summary.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>Selected or unmodeled source fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds complete bibliographic and status metadata for a patent application.
/// </summary>
public class PatentApplicationMetadata
{
    /// <summary>The canonical patent application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The patent number associated with the application.</summary>
    public string? PatentNumber = null;

    /// <summary>The pre-grant publication numbers associated with the application.</summary>
    public string[] PublicationNumbers = new string[0];

    /// <summary>The publication sequence numbers returned by the source.</summary>
    public string[] PublicationSequenceNumbers = new string[0];

    /// <summary>The publication categories returned by the source.</summary>
    public string[] PublicationCategories = new string[0];

    /// <summary>The publication dates returned by the source.</summary>
    public global::System.DateTimeOffset[] PublicationDates = new global::System.DateTimeOffset[0];

    /// <summary>The PCT application number associated with the application.</summary>
    public string? PctApplicationNumber = null;

    /// <summary>The PCT publication number associated with the application.</summary>
    public string? PctPublicationNumber = null;

    /// <summary>The international registration number.</summary>
    public string? InternationalRegistrationNumber = null;

    /// <summary>The invention title.</summary>
    public string? InventionTitle = null;

    /// <summary>The application type code.</summary>
    public string? ApplicationTypeCode = null;

    /// <summary>The application type label.</summary>
    public string? ApplicationTypeLabel = null;

    /// <summary>The broader application type category.</summary>
    public string? ApplicationTypeCategory = null;

    /// <summary>The application docket number.</summary>
    public string? DocketNumber = null;

    /// <summary>The application confirmation number, retained as an identifier string.</summary>
    public string? ConfirmationNumber = null;

    /// <summary>The correspondence customer number, retained as an identifier string.</summary>
    public string? CustomerNumber = null;

    /// <summary>The group art unit.</summary>
    public string? ArtUnit = null;

    /// <summary>The technology center.</summary>
    public string? TechnologyCenter = null;

    /// <summary>The filing date.</summary>
    public global::System.DateTimeOffset FilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FilingDate"/> was supplied.</summary>
    public bool HasFilingDate = false;

    /// <summary>The effective filing date.</summary>
    public global::System.DateTimeOffset EffectiveFilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="EffectiveFilingDate"/> was supplied.</summary>
    public bool HasEffectiveFilingDate = false;

    /// <summary>The application status date.</summary>
    public global::System.DateTimeOffset ApplicationStatusDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ApplicationStatusDate"/> was supplied.</summary>
    public bool HasApplicationStatusDate = false;

    /// <summary>The grant date.</summary>
    public global::System.DateTimeOffset GrantDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="GrantDate"/> was supplied.</summary>
    public bool HasGrantDate = false;

    /// <summary>The earliest pre-grant publication number.</summary>
    public string? EarliestPublicationNumber = null;

    /// <summary>The earliest pre-grant publication date.</summary>
    public global::System.DateTimeOffset EarliestPublicationDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="EarliestPublicationDate"/> was supplied.</summary>
    public bool HasEarliestPublicationDate = false;

    /// <summary>The PCT publication date.</summary>
    public global::System.DateTimeOffset PctPublicationDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="PctPublicationDate"/> was supplied.</summary>
    public bool HasPctPublicationDate = false;

    /// <summary>The international registration publication date.</summary>
    public global::System.DateTimeOffset InternationalRegistrationPublicationDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="InternationalRegistrationPublicationDate"/> was supplied.</summary>
    public bool HasInternationalRegistrationPublicationDate = false;

    /// <summary>The numeric application status code.</summary>
    public int ApplicationStatusCode = 0;

    /// <summary>Whether <see cref="ApplicationStatusCode"/> was supplied.</summary>
    public bool HasApplicationStatusCode = false;

    /// <summary>The application status description.</summary>
    public string? ApplicationStatusDescription = null;

    /// <summary>Whether the application entered the national stage from a PCT application.</summary>
    public bool IsNationalStage = false;

    /// <summary>Whether <see cref="IsNationalStage"/> was supplied.</summary>
    public bool HasIsNationalStage = false;

    /// <summary>Whether the application is governed by first-inventor-to-file rules.</summary>
    public bool IsFirstInventorToFile = false;

    /// <summary>Whether <see cref="IsFirstInventorToFile"/> was supplied.</summary>
    public bool HasIsFirstInventorToFile = false;

    /// <summary>The first applicant name reported by the source.</summary>
    public string? FirstApplicantName = null;

    /// <summary>The first inventor name reported by the source.</summary>
    public string? FirstInventorName = null;

    /// <summary>The applicant entity status.</summary>
    public PatentEntityStatus EntityStatus = new PatentEntityStatus();

    /// <summary>The parties and role records associated with the application.</summary>
    public PatentParty Parties = new PatentParty();

    /// <summary>The classifications and government-interest statements.</summary>
    public PatentClassificationData Classifications = new PatentClassificationData();

    /// <summary>Unmodeled metadata fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Describes one grant or pre-grant publication file associated with an application.
/// </summary>
public class PatentFileMetadata
{
    /// <summary>The source category, such as grant or pre-grant publication.</summary>
    public string? Category = null;

    /// <summary>The identifier of the related patent or publication.</summary>
    public string? DocumentIdentifier = null;

    /// <summary>The USPTO bulk product identifier.</summary>
    public string? ProductIdentifier = null;

    /// <summary>The source archive filename.</summary>
    public string? ArchiveFileName = null;

    /// <summary>The XML filename within or alongside the archive.</summary>
    public string? XmlFileName = null;

    /// <summary>The credential-free source location.</summary>
    public global::System.Uri? LocationUri = null;

    /// <summary>The source media type.</summary>
    public string? MediaType = null;

    /// <summary>The detected or declared document format.</summary>
    public DocumentFormat Format = DocumentFormat.Unknown;

    /// <summary>The source file creation time.</summary>
    public global::System.DateTimeOffset CreatedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="CreatedAt"/> was supplied.</summary>
    public bool HasCreatedAt = false;

    /// <summary>The source file size in bytes.</summary>
    public long Size = 0L;

    /// <summary>Whether <see cref="Size"/> was supplied.</summary>
    public bool HasSize = false;

    /// <summary>Unmodeled file metadata fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds the complete modeled Patent File Wrapper record for one application.
/// </summary>
public class PatentFileWrapper
{
    /// <summary>The canonical patent application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The compact summary derived from the wrapper.</summary>
    public PatentApplicationSummary Summary = new PatentApplicationSummary();

    /// <summary>The complete application metadata section.</summary>
    public PatentApplicationMetadata Metadata = new PatentApplicationMetadata();

    /// <summary>The pre-grant publication files associated with the application.</summary>
    public PatentFileMetadata[] PublicationFiles = new PatentFileMetadata[0];

    /// <summary>The grant files associated with the application.</summary>
    public PatentFileMetadata[] GrantFiles = new PatentFileMetadata[0];

    /// <summary>The source's last ingestion time for this wrapper.</summary>
    public global::System.DateTimeOffset LastIngestedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastIngestedAt"/> was supplied.</summary>
    public bool HasLastIngestedAt = false;

    /// <summary>The origin and authority of this wrapper.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>Unmodeled top-level sections retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownSections = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one value and count returned for a patent application facet.
/// </summary>
public class PatentApplicationFacetValue
{
    /// <summary>The facet value.</summary>
    public string? Value = null;

    /// <summary>The number of matching applications.</summary>
    public long Count = 0L;
}

/// <summary>
/// Holds all returned values for one patent application facet field.
/// </summary>
public class PatentApplicationFacet
{
    /// <summary>The application field that was aggregated.</summary>
    public string? Field = null;

    /// <summary>The returned facet values and counts.</summary>
    public PatentApplicationFacetValue[] Values = new PatentApplicationFacetValue[0];
}

/// <summary>
/// Holds one page or bounded aggregate of patent application results.
/// </summary>
public class PatentApplicationResponse
{
    /// <summary>The search request associated with the response.</summary>
    public SearchRequest Request = new SearchRequest();

    /// <summary>The compact search result summaries.</summary>
    public PatentApplicationSummary[] Summaries = new PatentApplicationSummary[0];

    /// <summary>The complete application wrappers returned by the operation.</summary>
    public PatentFileWrapper[] Applications = new PatentFileWrapper[0];

    /// <summary>The returned facet data.</summary>
    public PatentApplicationFacet[] Facets = new PatentApplicationFacet[0];

    /// <summary>The number of records held in this response.</summary>
    public int Count = 0;

    /// <summary>The service-reported total number of matches.</summary>
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
