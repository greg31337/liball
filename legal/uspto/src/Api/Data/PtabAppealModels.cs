namespace uspto;

/// <summary>
/// Holds lifecycle metadata for one PTAB appeal.
/// </summary>
public class PtabAppealMetadata
{
    /// <summary>The date on which the appeal was filed.</summary>
    public global::System.DateTimeOffset FilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FilingDate"/> was supplied.</summary>
    public bool HasFilingDate = false;

    /// <summary>The date on which the docket notice was mailed.</summary>
    public global::System.DateTimeOffset DocketNoticeMailedDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="DocketNoticeMailedDate"/> was supplied.</summary>
    public bool HasDocketNoticeMailedDate = false;

    /// <summary>The source-reported appeal modification date.</summary>
    public global::System.DateTimeOffset LastModifiedDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModifiedDate"/> was supplied.</summary>
    public bool HasLastModifiedDate = false;

    /// <summary>The source-reported appeal modification time.</summary>
    public global::System.DateTimeOffset LastModifiedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModifiedAt"/> was supplied.</summary>
    public bool HasLastModifiedAt = false;

    /// <summary>The application type or appeal disposition category.</summary>
    public string? ApplicationTypeCategory = null;

    /// <summary>The source-provided appeal file URI, when present.</summary>
    public global::System.Uri? FileDownloadUri = null;

    /// <summary>Unmodeled appeal metadata fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds patent, application, and representative data for a PTAB appellant.
/// </summary>
public class PtabAppellant
{
    /// <summary>The associated patent application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The associated patent number.</summary>
    public string? PatentNumber = null;

    /// <summary>The associated publication number.</summary>
    public string? PublicationNumber = null;

    /// <summary>The patent owner name.</summary>
    public string? PatentOwnerName = null;

    /// <summary>The real party in interest.</summary>
    public string? RealPartyInInterestName = null;

    /// <summary>The named inventor.</summary>
    public string? InventorName = null;

    /// <summary>The group art unit number.</summary>
    public string? ArtUnit = null;

    /// <summary>The technology center number.</summary>
    public string? TechnologyCenter = null;

    /// <summary>The patent publication date.</summary>
    public global::System.DateTimeOffset PublicationDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="PublicationDate"/> was supplied.</summary>
    public bool HasPublicationDate = false;

    /// <summary>The patent grant date.</summary>
    public global::System.DateTimeOffset GrantDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="GrantDate"/> was supplied.</summary>
    public bool HasGrantDate = false;

    /// <summary>The counsel reported for the appellant.</summary>
    public PtabCounsel Counsel = new PtabCounsel();

    /// <summary>Unmodeled appellant fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds a third-party requester associated with a PTAB appeal.
/// </summary>
public class PtabThirdPartyRequester
{
    /// <summary>The third-party requester name.</summary>
    public string? Name = null;

    /// <summary>Unmodeled requester fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds examiner data associated with a PTAB appeal.
/// </summary>
public class PtabAppealExaminer
{
    /// <summary>The examiner name.</summary>
    public string? Name = null;

    /// <summary>The examiner employee or service identifier.</summary>
    public string? Identifier = null;

    /// <summary>The group art unit number.</summary>
    public string? ArtUnit = null;

    /// <summary>The technology center number.</summary>
    public string? TechnologyCenter = null;

    /// <summary>Unmodeled examiner fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one PTAB appeal decision and its application context.
/// </summary>
public class PtabAppealDecision
{
    /// <summary>The PTAB appeal number.</summary>
    public string? AppealNumber = null;

    /// <summary>The appeal document category.</summary>
    public string? RecordCategory = null;

    /// <summary>The source-reported record modification time.</summary>
    public global::System.DateTimeOffset LastModifiedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModifiedAt"/> was supplied.</summary>
    public bool HasLastModifiedAt = false;

    /// <summary>The appeal lifecycle metadata.</summary>
    public PtabAppealMetadata Metadata = new PtabAppealMetadata();

    /// <summary>The appellant data.</summary>
    public PtabAppellant Appellant = new PtabAppellant();

    /// <summary>The third-party requester data.</summary>
    public PtabThirdPartyRequester ThirdPartyRequester = new PtabThirdPartyRequester();

    /// <summary>The examiner data.</summary>
    public PtabAppealExaminer Examiner = new PtabAppealExaminer();

    /// <summary>The appeal decision document.</summary>
    public PtabDocument Document = new PtabDocument();

    /// <summary>The appeal decision outcome and authority data.</summary>
    public PtabDecision Decision = new PtabDecision();

    /// <summary>The origin and authority of this appeal decision.</summary>
    public PtabDataProvenance Provenance = new PtabDataProvenance();

    /// <summary>Unmodeled appeal decision fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one page or bounded aggregate of PTAB appeal decisions.
/// </summary>
public class PtabAppealDecisionResponse
{
    /// <summary>The search request associated with the response.</summary>
    public SearchRequest Request = new SearchRequest();

    /// <summary>The returned appeal decisions.</summary>
    public PtabAppealDecision[] Decisions = new PtabAppealDecision[0];

    /// <summary>The returned facet data.</summary>
    public PtabFacet[] Facets = new PtabFacet[0];

    /// <summary>The number of appeal decisions held in this response.</summary>
    public int Count = 0;

    /// <summary>The service-reported total number of matching appeal decisions.</summary>
    public long Total = 0L;

    /// <summary>Whether <see cref="Total"/> was supplied.</summary>
    public bool HasTotal = false;

    /// <summary>The offset-page coordinates associated with the response.</summary>
    public OffsetPage Page = new OffsetPage();

    /// <summary>The service request identifier.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and authority of the response.</summary>
    public PtabDataProvenance Provenance = new PtabDataProvenance();

    /// <summary>A bounded retained raw response, when requested.</summary>
    public string? RawResponse = null;

    /// <summary>Whether all requested pages were processed.</summary>
    public bool IsComplete = false;
}
