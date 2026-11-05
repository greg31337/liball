namespace uspto;

/// <summary>
/// Holds one United States patent, application, foreign-document, or other PatentsView citation.
/// </summary>
public class PatentsViewCitation
{
    /// <summary>The citing granted patent identifier.</summary>
    public string? PatentIdentifier = null;

    /// <summary>The citing pre-grant publication document number.</summary>
    public string? DocumentNumber = null;

    /// <summary>The API resource reference for the citing record.</summary>
    public string? SourceResourceReference = null;

    /// <summary>The United States patent, application, foreign, or other citation category.</summary>
    public string? CitationType = null;

    /// <summary>The cited United States patent identifier.</summary>
    public string? CitedPatentIdentifier = null;

    /// <summary>The cited pre-grant publication document number.</summary>
    public string? CitedDocumentNumber = null;

    /// <summary>The cited foreign or source document number.</summary>
    public string? CitationNumber = null;

    /// <summary>The API resource reference for the cited record.</summary>
    public string? CitedResourceReference = null;

    /// <summary>The examiner, applicant, or other citing-party category.</summary>
    public string? CitationCategory = null;

    /// <summary>The cited document date.</summary>
    public global::System.DateTimeOffset CitationDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="CitationDate"/> was supplied.</summary>
    public bool HasCitationDate = false;

    /// <summary>The source name associated with the citation.</summary>
    public string? CitationName = null;

    /// <summary>The cited document country.</summary>
    public string? Country = null;

    /// <summary>The cited document's WIPO kind code.</summary>
    public string? KindCode = null;

    /// <summary>The citation or reference order in the source document.</summary>
    public int Sequence = 0;

    /// <summary>Whether <see cref="Sequence"/> was supplied.</summary>
    public bool HasSequence = false;

    /// <summary>The verbatim non-patent or other-reference text.</summary>
    public string? ReferenceText = null;

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled citation fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds related-application text for a patent or pre-grant publication.
/// </summary>
public class PatentsViewRelatedApplication
{
    /// <summary>The related granted patent identifier.</summary>
    public string? PatentIdentifier = null;

    /// <summary>The related pre-grant publication document number.</summary>
    public string? DocumentNumber = null;

    /// <summary>The source description of the related application.</summary>
    public string? RelatedText = null;

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled related-application fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one claim from a PatentsView grant or pre-grant publication text table.
/// </summary>
public class PatentsViewClaim
{
    /// <summary>The related granted patent identifier.</summary>
    public string? PatentIdentifier = null;

    /// <summary>The related pre-grant publication document number.</summary>
    public string? DocumentNumber = null;

    /// <summary>The source claim number or cancelled-claim range.</summary>
    public string? ClaimNumber = null;

    /// <summary>The claim's source order.</summary>
    public int Sequence = 0;

    /// <summary>Whether <see cref="Sequence"/> was supplied.</summary>
    public bool HasSequence = false;

    /// <summary>The complete claim text.</summary>
    public string? Text = null;

    /// <summary>Whether the source marks the claim as exemplary.</summary>
    public bool IsExemplary = false;

    /// <summary>Whether <see cref="IsExemplary"/> was supplied.</summary>
    public bool HasIsExemplary = false;

    /// <summary>The source number or sequence of the claim on which this claim depends.</summary>
    public string? DependsOnClaim = null;

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled claim fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds brief-summary text for a PatentsView grant or pre-grant publication.
/// </summary>
public class PatentsViewSummary
{
    /// <summary>The related granted patent identifier.</summary>
    public string? PatentIdentifier = null;

    /// <summary>The related pre-grant publication document number.</summary>
    public string? DocumentNumber = null;

    /// <summary>The complete brief-summary text.</summary>
    public string? Text = null;

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled summary fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds detailed-description text for a PatentsView grant or pre-grant publication.
/// </summary>
public class PatentsViewDescription
{
    /// <summary>The related granted patent identifier.</summary>
    public string? PatentIdentifier = null;

    /// <summary>The related pre-grant publication document number.</summary>
    public string? DocumentNumber = null;

    /// <summary>The complete detailed-description text.</summary>
    public string? Text = null;

    /// <summary>The reported description length in characters.</summary>
    public int Length = 0;

    /// <summary>Whether <see cref="Length"/> was supplied.</summary>
    public bool HasLength = false;

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled description fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one drawing-description entry for a PatentsView grant or pre-grant publication.
/// </summary>
public class PatentsViewDrawingDescription
{
    /// <summary>The related granted patent identifier.</summary>
    public string? PatentIdentifier = null;

    /// <summary>The related pre-grant publication document number.</summary>
    public string? DocumentNumber = null;

    /// <summary>The drawing description's source order or figure identifier.</summary>
    public int Sequence = 0;

    /// <summary>Whether <see cref="Sequence"/> was supplied.</summary>
    public bool HasSequence = false;

    /// <summary>The complete drawing-description text.</summary>
    public string? Text = null;

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled drawing-description fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one page or bounded aggregate of PatentsView citations.
/// </summary>
public class PatentsViewCitationResponse
{
    /// <summary>The returned citations in stable source order.</summary>
    public PatentsViewCitation[] Citations = new PatentsViewCitation[0];

    /// <summary>The query, counts, cursor, raw response, and research provenance.</summary>
    public PatentsViewResponseMetadata Metadata = new PatentsViewResponseMetadata();
}

/// <summary>
/// Holds one page or bounded aggregate of PatentsView related-application records.
/// </summary>
public class PatentsViewRelatedApplicationResponse
{
    /// <summary>The returned related-application records in stable source order.</summary>
    public PatentsViewRelatedApplication[] RelatedApplications = new PatentsViewRelatedApplication[0];

    /// <summary>The query, counts, cursor, raw response, and research provenance.</summary>
    public PatentsViewResponseMetadata Metadata = new PatentsViewResponseMetadata();
}

/// <summary>
/// Holds one page or bounded aggregate of PatentsView claims.
/// </summary>
public class PatentsViewClaimResponse
{
    /// <summary>The returned claims in stable source order.</summary>
    public PatentsViewClaim[] Claims = new PatentsViewClaim[0];

    /// <summary>The query, counts, cursor, raw response, and research provenance.</summary>
    public PatentsViewResponseMetadata Metadata = new PatentsViewResponseMetadata();
}

/// <summary>
/// Holds one page or bounded aggregate of PatentsView brief summaries.
/// </summary>
public class PatentsViewSummaryResponse
{
    /// <summary>The returned brief summaries in stable source order.</summary>
    public PatentsViewSummary[] Summaries = new PatentsViewSummary[0];

    /// <summary>The query, counts, cursor, raw response, and research provenance.</summary>
    public PatentsViewResponseMetadata Metadata = new PatentsViewResponseMetadata();
}

/// <summary>
/// Holds one page or bounded aggregate of PatentsView detailed descriptions.
/// </summary>
public class PatentsViewDescriptionResponse
{
    /// <summary>The returned detailed descriptions in stable source order.</summary>
    public PatentsViewDescription[] Descriptions = new PatentsViewDescription[0];

    /// <summary>The query, counts, cursor, raw response, and research provenance.</summary>
    public PatentsViewResponseMetadata Metadata = new PatentsViewResponseMetadata();
}

/// <summary>
/// Holds one page or bounded aggregate of PatentsView drawing descriptions.
/// </summary>
public class PatentsViewDrawingDescriptionResponse
{
    /// <summary>The returned drawing descriptions in stable source order.</summary>
    public PatentsViewDrawingDescription[] DrawingDescriptions = new PatentsViewDrawingDescription[0];

    /// <summary>The query, counts, cursor, raw response, and research provenance.</summary>
    public PatentsViewResponseMetadata Metadata = new PatentsViewResponseMetadata();
}
