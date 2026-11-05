namespace uspto;

/// <summary>
/// Holds one source-specific citation record prepared for graph construction.
/// </summary>
public class CitationGraphEvidence
{
    /// <summary>The original data source.</summary>
    public DataProvenance Source = DataProvenance.Unknown;

    /// <summary>The citation direction relative to <see cref="SourceIdentifier"/>.</summary>
    public CitationDirection Direction = CitationDirection.Unknown;

    /// <summary>The normalized cited-document or literature category.</summary>
    public CitationDocumentCategory DocumentCategory = CitationDocumentCategory.Unknown;

    /// <summary>The source document or application identifier.</summary>
    public string? SourceIdentifier = null;

    /// <summary>The normalized cited-document identifier, when available.</summary>
    public string? TargetIdentifier = null;

    /// <summary>The source-provided examiner, applicant, or other citation category.</summary>
    public string? CitationCategory = null;

    /// <summary>The verbatim cited-reference text.</summary>
    public string? ReferenceText = null;

    /// <summary>The verbatim non-patent literature text, when applicable.</summary>
    public string? NonPatentLiterature = null;

    /// <summary>The originating Office Action citation, when this evidence came from that dataset.</summary>
    public OfficeActionCitation? OfficeActionCitation = null;

    /// <summary>The originating PatentsView citation, when this evidence came from that dataset.</summary>
    public PatentsViewCitation? PatentsViewCitation = null;

    /// <summary>The source record's complete provenance.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();
}

/// <summary>
/// Holds one citation graph input and all source records matched to it.
/// </summary>
public class CitationGraphInput
{
    /// <summary>The normalized cited-document identifier, when available.</summary>
    public string? TargetIdentifier = null;

    /// <summary>The normalized citation direction shared by the source evidence.</summary>
    public CitationDirection Direction = CitationDirection.Unknown;

    /// <summary>The normalized document or literature category shared by the source evidence.</summary>
    public CitationDocumentCategory DocumentCategory = CitationDocumentCategory.Unknown;

    /// <summary>The preferred verbatim non-patent literature text, when applicable.</summary>
    public string? NonPatentLiterature = null;

    /// <summary>The confidence that multiple evidence records refer to the same citation.</summary>
    public CitationMatchConfidence MatchConfidence = CitationMatchConfidence.None;

    /// <summary>The normalized fields that supported the evidence match.</summary>
    public string? MatchBasis = null;

    /// <summary>The source-specific records retained for this graph input.</summary>
    public CitationGraphEvidence[] Evidence = new CitationGraphEvidence[0];

    /// <summary>The non-authoritative client provenance of the combined graph input.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();
}

/// <summary>
/// Holds deterministic citation graph inputs composed from official and research sources.
/// </summary>
public class CitationGraphInputSet
{
    /// <summary>The combined graph inputs in stable source order.</summary>
    public CitationGraphInput[] Inputs = new CitationGraphInput[0];

    /// <summary>The number of combined graph inputs.</summary>
    public int Count = 0;

    /// <summary>The number of inputs supported by both Office Action and PatentsView evidence.</summary>
    public int MatchedCount = 0;

    /// <summary>The number of Office Action records without matching PatentsView evidence.</summary>
    public int UnmatchedOfficeActionCount = 0;

    /// <summary>The number of PatentsView records without matching Office Action evidence.</summary>
    public int UnmatchedPatentsViewCount = 0;

    /// <summary>The number of citation sources supplied successfully.</summary>
    public int LoadedSourceCount = 0;

    /// <summary>The number of citation sources that failed or were unavailable.</summary>
    public int FailedSourceCount = 0;

    /// <summary>The source failures retained while composing available citation evidence.</summary>
    public SectionError[] Errors = new SectionError[0];

    /// <summary>Whether both citation sources were supplied successfully.</summary>
    public bool IsComplete = false;

    /// <summary>Whether citation inputs were retained alongside at least one source failure.</summary>
    public bool HasPartialSuccess = false;

    /// <summary>The non-authoritative client provenance of the composition.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();
}

/// <summary>
/// Selects available citation responses and captures failures from unavailable sources.
/// </summary>
public class CitationGraphAggregationRequest
{
    /// <summary>The normalized official Office Action response, when loading succeeded.</summary>
    public OfficeActionCitationResponse? OfficeActionCitations = null;

    /// <summary>The research-derived PatentsView response, when loading succeeded.</summary>
    public PatentsViewCitationResponse? PatentsViewCitations = null;

    /// <summary>The Office Action source failure, when loading did not succeed.</summary>
    public SectionError? OfficeActionError = null;

    /// <summary>The PatentsView source failure, when loading did not succeed.</summary>
    public SectionError? PatentsViewError = null;
}
