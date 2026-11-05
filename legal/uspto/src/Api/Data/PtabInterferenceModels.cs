namespace uspto;

/// <summary>
/// Holds lifecycle metadata for one PTAB interference.
/// </summary>
public class PtabInterferenceMetadata
{
    /// <summary>The interference style name.</summary>
    public string? StyleName = null;

    /// <summary>The source-reported interference modification date.</summary>
    public global::System.DateTimeOffset LastModifiedDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModifiedDate"/> was supplied.</summary>
    public bool HasLastModifiedDate = false;

    /// <summary>The source-provided interference file URI, when present.</summary>
    public global::System.Uri? FileDownloadUri = null;

    /// <summary>Unmodeled interference metadata fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one senior, junior, or additional party in a PTAB interference.
/// </summary>
public class PtabInterferenceParty
{
    /// <summary>The party category, such as Senior, Junior, or Additional.</summary>
    public string? Category = null;

    /// <summary>The party name when supplied separately from patent-owner data.</summary>
    public string? Name = null;

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

    /// <summary>The counsel reported for the party.</summary>
    public PtabCounsel Counsel = new PtabCounsel();

    /// <summary>Unmodeled party fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one PTAB interference decision and its parties.
/// </summary>
public class PtabInterferenceDecision
{
    /// <summary>The PTAB interference number.</summary>
    public string? InterferenceNumber = null;

    /// <summary>The interference document category.</summary>
    public string? RecordCategory = null;

    /// <summary>The source-reported record modification time.</summary>
    public global::System.DateTimeOffset LastModifiedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModifiedAt"/> was supplied.</summary>
    public bool HasLastModifiedAt = false;

    /// <summary>The interference lifecycle metadata.</summary>
    public PtabInterferenceMetadata Metadata = new PtabInterferenceMetadata();

    /// <summary>The senior interference party.</summary>
    public PtabInterferenceParty SeniorParty = new PtabInterferenceParty();

    /// <summary>The junior interference party.</summary>
    public PtabInterferenceParty JuniorParty = new PtabInterferenceParty();

    /// <summary>The additional interference parties.</summary>
    public PtabInterferenceParty[] AdditionalParties = new PtabInterferenceParty[0];

    /// <summary>The interference decision document.</summary>
    public PtabDocument Document = new PtabDocument();

    /// <summary>The interference decision outcome and authority data.</summary>
    public PtabDecision Decision = new PtabDecision();

    /// <summary>The origin and authority of this interference decision.</summary>
    public PtabDataProvenance Provenance = new PtabDataProvenance();

    /// <summary>Unmodeled interference decision fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one page or bounded aggregate of PTAB interference decisions.
/// </summary>
public class PtabInterferenceDecisionResponse
{
    /// <summary>The search request associated with the response.</summary>
    public SearchRequest Request = new SearchRequest();

    /// <summary>The returned interference decisions.</summary>
    public PtabInterferenceDecision[] Decisions = new PtabInterferenceDecision[0];

    /// <summary>The returned facet data.</summary>
    public PtabFacet[] Facets = new PtabFacet[0];

    /// <summary>The number of interference decisions held in this response.</summary>
    public int Count = 0;

    /// <summary>The service-reported total number of matching interference decisions.</summary>
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
