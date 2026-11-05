namespace uspto;

/// <summary>
/// Describes the current availability of the configured PatentsView PatentSearch service and its durable bulk alternatives.
/// </summary>
public class PatentsViewCapabilityResult
{
    /// <summary>The human-readable capability name.</summary>
    public string? CapabilityName = null;

    /// <summary>The credential-free configured PatentSearch base URI.</summary>
    public global::System.Uri? BaseUri = null;

    /// <summary>Whether the configured PatentSearch service accepted a minimal query.</summary>
    public bool IsAvailable = false;

    /// <summary>Whether direct online PatentSearch operations are currently supported.</summary>
    public bool SupportsOnlineSearch = false;

    /// <summary>Whether PatentSearch requires an API key.</summary>
    public bool RequiresApiKey = true;

    /// <summary>Whether the unavailable result is consistent with a paused or moved transition endpoint.</summary>
    public bool IsTransitionUnavailable = false;

    /// <summary>The stable reason code when online access is unavailable.</summary>
    public string? UnavailabilityCode = null;

    /// <summary>A credential-free explanation of the online availability state.</summary>
    public string? UnavailabilityReason = null;

    /// <summary>The HTTP status code returned by the probe.</summary>
    public int StatusCode = 0;

    /// <summary>Whether <see cref="StatusCode"/> contains a received value.</summary>
    public bool HasStatusCode = false;

    /// <summary>The service request identifier returned by the probe.</summary>
    public string? RequestIdentifier = null;

    /// <summary>Whether retry policy classifies the final failure as transient.</summary>
    public bool IsRetryable = false;

    /// <summary>The server-requested retry delay in milliseconds.</summary>
    public long RetryAfterMilliseconds = 0L;

    /// <summary>Whether <see cref="RetryAfterMilliseconds"/> contains a server-provided delay.</summary>
    public bool HasRetryAfterMilliseconds = false;

    /// <summary>The number of network attempts made by this probe.</summary>
    public int AttemptCount = 0;

    /// <summary>Whether the capability response came from a response cache.</summary>
    public bool FromCache = false;

    /// <summary>Whether related PatentsView products can be discovered through the generic bulk catalog.</summary>
    public bool IsBulkDiscoveryAvailable = true;

    /// <summary>The bulk dataset families that provide durable PatentsView-derived access.</summary>
    public DatasetFamily[] BulkDatasetFamilies = new DatasetFamily[0];

    /// <summary>A ready-to-use generic bulk catalog search for PatentsView products.</summary>
    public SearchRequest BulkProductSearch = new SearchRequest();

    /// <summary>The research-derived source and retrieval context of the capability determination.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>A bounded raw service response retained at the caller's request.</summary>
    public string? RawResponse = null;
}

/// <summary>
/// Describes one typed PatentsView PatentSearch query and its service-specific options.
/// </summary>
public class PatentsViewQueryRequest
{
    /// <summary>The structured search, selected fields, stable sorts, cursor, and request context.</summary>
    public SearchRequest Search = new SearchRequest
    {
        UseCursorPage = true,
        Context = new RequestContext { Service = ApiService.PatentsView }
    };

    /// <summary>Whether returned patent identifiers should be padded for natural sorting.</summary>
    public bool PadPatentIdentifiers = false;

    /// <summary>Whether withdrawn grants should be excluded from patent queries.</summary>
    public bool ExcludeWithdrawnPatents = true;
}

/// <summary>
/// Holds the core application linkage embedded in a PatentsView patent record.
/// </summary>
public class PatentsViewPatentApplication
{
    /// <summary>The application identifier assigned by the USPTO.</summary>
    public string? ApplicationIdentifier = null;

    /// <summary>The application type or category.</summary>
    public string? ApplicationType = null;

    /// <summary>The application filing date.</summary>
    public global::System.DateTimeOffset FilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FilingDate"/> was supplied.</summary>
    public bool HasFilingDate = false;

    /// <summary>The source filing-type value.</summary>
    public string? FilingType = null;

    /// <summary>The application series code, retained as identifier text.</summary>
    public string? SeriesCode = null;

    /// <summary>Whether the application was filed under Rule 47.</summary>
    public bool Rule47 = false;

    /// <summary>Whether <see cref="Rule47"/> was supplied.</summary>
    public bool HasRule47 = false;

    /// <summary>The explicitly research-derived source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Selected or unmodeled source fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds the core scalar and application-linkage fields for one PatentsView granted patent.
/// </summary>
public class PatentsViewPatent
{
    /// <summary>The patent identifier in the source's padded or unpadded form.</summary>
    public string? PatentIdentifier = null;

    /// <summary>The patent title.</summary>
    public string? Title = null;

    /// <summary>The patent abstract.</summary>
    public string? Abstract = null;

    /// <summary>The patent grant date.</summary>
    public global::System.DateTimeOffset GrantDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="GrantDate"/> was supplied.</summary>
    public bool HasGrantDate = false;

    /// <summary>The grant year reported by the source.</summary>
    public int GrantYear = 0;

    /// <summary>Whether <see cref="GrantYear"/> was supplied.</summary>
    public bool HasGrantYear = false;

    /// <summary>The utility, design, plant, reissue, or other patent category.</summary>
    public string? PatentType = null;

    /// <summary>The WIPO document kind code.</summary>
    public string? KindCode = null;

    /// <summary>Whether the patent has been withdrawn.</summary>
    public bool IsWithdrawn = false;

    /// <summary>Whether <see cref="IsWithdrawn"/> was supplied.</summary>
    public bool HasIsWithdrawn = false;

    /// <summary>The earliest filing date among applications from which priority is claimed.</summary>
    public global::System.DateTimeOffset EarliestApplicationDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="EarliestApplicationDate"/> was supplied.</summary>
    public bool HasEarliestApplicationDate = false;

    /// <summary>The detailed-description length in characters.</summary>
    public int DescriptionLength = 0;

    /// <summary>Whether <see cref="DescriptionLength"/> was supplied.</summary>
    public bool HasDescriptionLength = false;

    /// <summary>The number of cited United States patents.</summary>
    public int UnitedStatesPatentCitationCount = 0;

    /// <summary>Whether <see cref="UnitedStatesPatentCitationCount"/> was supplied.</summary>
    public bool HasUnitedStatesPatentCitationCount = false;

    /// <summary>The number of cited United States applications.</summary>
    public int UnitedStatesApplicationCitationCount = 0;

    /// <summary>Whether <see cref="UnitedStatesApplicationCitationCount"/> was supplied.</summary>
    public bool HasUnitedStatesApplicationCitationCount = false;

    /// <summary>The number of cited foreign patent documents.</summary>
    public int ForeignDocumentCitationCount = 0;

    /// <summary>Whether <see cref="ForeignDocumentCitationCount"/> was supplied.</summary>
    public bool HasForeignDocumentCitationCount = false;

    /// <summary>The total number of cited documents.</summary>
    public int TotalDocumentCitationCount = 0;

    /// <summary>Whether <see cref="TotalDocumentCitationCount"/> was supplied.</summary>
    public bool HasTotalDocumentCitationCount = false;

    /// <summary>The number of subsequent United States patents citing this patent.</summary>
    public int CitedByUnitedStatesPatentCount = 0;

    /// <summary>Whether <see cref="CitedByUnitedStatesPatentCount"/> was supplied.</summary>
    public bool HasCitedByUnitedStatesPatentCount = false;

    /// <summary>The prosecution duration in days.</summary>
    public int ProcessingDays = 0;

    /// <summary>Whether <see cref="ProcessingDays"/> was supplied.</summary>
    public bool HasProcessingDays = false;

    /// <summary>The patent-term extension in days.</summary>
    public int TermExtensionDays = 0;

    /// <summary>Whether <see cref="TermExtensionDays"/> was supplied.</summary>
    public bool HasTermExtensionDays = false;

    /// <summary>The core application records linked to the patent.</summary>
    public PatentsViewPatentApplication[] Applications = new PatentsViewPatentApplication[0];

    /// <summary>The applicants associated with the patent.</summary>
    public PatentsViewApplicant[] Applicants = new PatentsViewApplicant[0];

    /// <summary>The disambiguated inventors associated with the patent.</summary>
    public PatentsViewInventor[] Inventors = new PatentsViewInventor[0];

    /// <summary>The disambiguated assignees associated with the patent.</summary>
    public PatentsViewAssignee[] Assignees = new PatentsViewAssignee[0];

    /// <summary>The patent attorneys associated with the patent.</summary>
    public PatentsViewAttorney[] Attorneys = new PatentsViewAttorney[0];

    /// <summary>The government-interest statements associated with the patent.</summary>
    public PatentsViewGovernmentInterest[] GovernmentInterests = new PatentsViewGovernmentInterest[0];

    /// <summary>The grant-to-publication mappings associated with the patent.</summary>
    public PatentsViewGrantPublicationCrosswalk[] PublicationCrosswalks = new PatentsViewGrantPublicationCrosswalk[0];

    /// <summary>The CPC classifications associated with the patent.</summary>
    public PatentsViewCpcClassification[] CpcClassifications = new PatentsViewCpcClassification[0];

    /// <summary>The IPC classifications associated with the patent.</summary>
    public PatentsViewIpcClassification[] IpcClassifications = new PatentsViewIpcClassification[0];

    /// <summary>The USPC classifications associated with the patent.</summary>
    public PatentsViewUspcClassification[] UspcClassifications = new PatentsViewUspcClassification[0];

    /// <summary>The WIPO technology fields associated with the patent.</summary>
    public PatentsViewWipoClassification[] WipoClassifications = new PatentsViewWipoClassification[0];

    /// <summary>The patent, application, foreign-document, and other citations.</summary>
    public PatentsViewCitation[] Citations = new PatentsViewCitation[0];

    /// <summary>The related-application text records.</summary>
    public PatentsViewRelatedApplication[] RelatedApplications = new PatentsViewRelatedApplication[0];

    /// <summary>The patent claims in source order.</summary>
    public PatentsViewClaim[] Claims = new PatentsViewClaim[0];

    /// <summary>The brief summary, when loaded.</summary>
    public PatentsViewSummary? Summary = null;

    /// <summary>The detailed description, when loaded.</summary>
    public PatentsViewDescription? Description = null;

    /// <summary>The drawing descriptions in source order.</summary>
    public PatentsViewDrawingDescription[] DrawingDescriptions = new PatentsViewDrawingDescription[0];

    /// <summary>The explicitly research-derived source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Selected or unmodeled source fields retained as raw scalar or JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds the core scalar fields for one PatentsView pre-grant publication.
/// </summary>
public class PatentsViewPublication
{
    /// <summary>The unique pre-grant document number.</summary>
    public string? DocumentNumber = null;

    /// <summary>The publication title.</summary>
    public string? Title = null;

    /// <summary>The publication abstract.</summary>
    public string? Abstract = null;

    /// <summary>The publication date.</summary>
    public global::System.DateTimeOffset PublicationDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="PublicationDate"/> was supplied.</summary>
    public bool HasPublicationDate = false;

    /// <summary>The publication year reported by the source.</summary>
    public int PublicationYear = 0;

    /// <summary>Whether <see cref="PublicationYear"/> was supplied.</summary>
    public bool HasPublicationYear = false;

    /// <summary>The publication type or category.</summary>
    public string? PublicationType = null;

    /// <summary>The application series code, retained as identifier text.</summary>
    public string? SeriesCode = null;

    /// <summary>Whether the underlying application was filed under Rule 47.</summary>
    public bool Rule47 = false;

    /// <summary>Whether <see cref="Rule47"/> was supplied.</summary>
    public bool HasRule47 = false;

    /// <summary>The applicants associated with the publication.</summary>
    public PatentsViewApplicant[] Applicants = new PatentsViewApplicant[0];

    /// <summary>The disambiguated inventors associated with the publication.</summary>
    public PatentsViewInventor[] Inventors = new PatentsViewInventor[0];

    /// <summary>The disambiguated assignees associated with the publication.</summary>
    public PatentsViewAssignee[] Assignees = new PatentsViewAssignee[0];

    /// <summary>The government-interest statements associated with the publication.</summary>
    public PatentsViewGovernmentInterest[] GovernmentInterests = new PatentsViewGovernmentInterest[0];

    /// <summary>The publication-to-grant mappings associated with the publication.</summary>
    public PatentsViewGrantPublicationCrosswalk[] GrantCrosswalks = new PatentsViewGrantPublicationCrosswalk[0];

    /// <summary>The CPC classifications associated with the publication.</summary>
    public PatentsViewCpcClassification[] CpcClassifications = new PatentsViewCpcClassification[0];

    /// <summary>The IPC classifications associated with the publication.</summary>
    public PatentsViewIpcClassification[] IpcClassifications = new PatentsViewIpcClassification[0];

    /// <summary>The USPC classifications associated with the publication.</summary>
    public PatentsViewUspcClassification[] UspcClassifications = new PatentsViewUspcClassification[0];

    /// <summary>The WIPO technology fields associated with the publication.</summary>
    public PatentsViewWipoClassification[] WipoClassifications = new PatentsViewWipoClassification[0];

    /// <summary>The citations associated with the publication.</summary>
    public PatentsViewCitation[] Citations = new PatentsViewCitation[0];

    /// <summary>The related-application text records.</summary>
    public PatentsViewRelatedApplication[] RelatedApplications = new PatentsViewRelatedApplication[0];

    /// <summary>The publication claims in source order.</summary>
    public PatentsViewClaim[] Claims = new PatentsViewClaim[0];

    /// <summary>The brief summary, when loaded.</summary>
    public PatentsViewSummary? Summary = null;

    /// <summary>The detailed description, when loaded.</summary>
    public PatentsViewDescription? Description = null;

    /// <summary>The drawing descriptions in source order.</summary>
    public PatentsViewDrawingDescription[] DrawingDescriptions = new PatentsViewDrawingDescription[0];

    /// <summary>The explicitly research-derived source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Selected or unmodeled source fields retained as raw scalar or JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds metadata common to typed PatentsView response pages and bounded aggregates.
/// </summary>
public class PatentsViewResponseMetadata
{
    /// <summary>The typed query associated with the response.</summary>
    public PatentsViewQueryRequest Request = new PatentsViewQueryRequest();

    /// <summary>Whether the service marked the response as an error.</summary>
    public bool IsError = false;

    /// <summary>The number of records returned in this response.</summary>
    public int Count = 0;

    /// <summary>The total number of records matching the query.</summary>
    public long TotalHits = 0L;

    /// <summary>Whether <see cref="TotalHits"/> was supplied.</summary>
    public bool HasTotalHits = false;

    /// <summary>The stable sort values for the next cursor page.</summary>
    public CursorPage NextCursorPage = new CursorPage();

    /// <summary>Whether <see cref="NextCursorPage"/> contains a following-page cursor.</summary>
    public bool HasNextCursorPage = false;

    /// <summary>Whether all records permitted by the query bounds were collected.</summary>
    public bool IsComplete = false;

    /// <summary>The service request identifier, when supplied.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The explicitly research-derived source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>A bounded raw service response retained at the caller's request.</summary>
    public string? RawResponse = null;

    /// <summary>Unmodeled response fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one page or bounded aggregate of PatentsView granted patents.
/// </summary>
public class PatentsViewPatentResponse
{
    /// <summary>The returned patent records in stable query order.</summary>
    public PatentsViewPatent[] Patents = new PatentsViewPatent[0];

    /// <summary>The query, counts, cursor, raw response, and research provenance.</summary>
    public PatentsViewResponseMetadata Metadata = new PatentsViewResponseMetadata();
}

/// <summary>
/// Holds one page or bounded aggregate of PatentsView pre-grant publications.
/// </summary>
public class PatentsViewPublicationResponse
{
    /// <summary>The returned publication records in stable query order.</summary>
    public PatentsViewPublication[] Publications = new PatentsViewPublication[0];

    /// <summary>The query, counts, cursor, raw response, and research provenance.</summary>
    public PatentsViewResponseMetadata Metadata = new PatentsViewResponseMetadata();
}
