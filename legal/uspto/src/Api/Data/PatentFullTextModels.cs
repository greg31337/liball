namespace uspto;

/// <summary>
/// Identifies a patent document using the ICE or ST.36 document-id components.
/// </summary>
public class PatentDocumentIdentifier
{
    /// <summary>The issuing or receiving office country code.</summary>
    public string? CountryCode = null;

    /// <summary>The document number in source form.</summary>
    public string? DocumentNumber = null;

    /// <summary>The publication kind code.</summary>
    public string? KindCode = null;

    /// <summary>The person or organization name supplied with the identifier.</summary>
    public string? Name = null;

    /// <summary>The source date text, including partially specified dates.</summary>
    public string? DateText = null;

    /// <summary>The parsed document date.</summary>
    public global::System.DateTimeOffset Date = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="Date"/> was parsed from a complete valid date.</summary>
    public bool HasDate = false;
}

/// <summary>
/// Holds one ordered abstract or description block.
/// </summary>
public class PatentTextBlock
{
    /// <summary>The source element identifier.</summary>
    public string? Identifier = null;

    /// <summary>The source paragraph or block number.</summary>
    public string? Number = null;

    /// <summary>The block category, such as paragraph or heading.</summary>
    public string? Category = null;

    /// <summary>The heading level supplied by the source.</summary>
    public string? Level = null;

    /// <summary>The normalized text content.</summary>
    public string? Text = null;
}

/// <summary>
/// Holds one claim reference retained from claim text.
/// </summary>
public class PatentClaimReference
{
    /// <summary>The referenced source claim identifier.</summary>
    public string? Identifier = null;

    /// <summary>The claim-reference text.</summary>
    public string? Text = null;
}

/// <summary>
/// Holds one patent claim and its referenced claims.
/// </summary>
public class PatentClaim
{
    /// <summary>The source claim identifier.</summary>
    public string? Identifier = null;

    /// <summary>The source claim number.</summary>
    public string? Number = null;

    /// <summary>The normalized complete claim text.</summary>
    public string? Text = null;

    /// <summary>The claim references occurring in the claim.</summary>
    public PatentClaimReference[] References = new PatentClaimReference[0];
}

/// <summary>
/// Holds one patent or non-patent citation.
/// </summary>
public class PatentCitation
{
    /// <summary>The source citation number.</summary>
    public string? Number = null;

    /// <summary>The cited patent document, when this is a patent citation.</summary>
    public PatentDocumentIdentifier? PatentDocument = null;

    /// <summary>The complete non-patent literature citation.</summary>
    public string? NonPatentText = null;

    /// <summary>The source citation category.</summary>
    public string? Category = null;

    /// <summary>The claims associated with the citation.</summary>
    public string? RelatedClaims = null;

    /// <summary>The classification text associated with the citation.</summary>
    public string[] ClassificationText = new string[0];
}

/// <summary>
/// Holds one related-publication, parent, child, or other application relationship.
/// </summary>
public class PatentRelatedApplication
{
    /// <summary>The source relationship element name.</summary>
    public string? Relationship = null;

    /// <summary>The directly related document identifier.</summary>
    public PatentDocumentIdentifier? Document = null;

    /// <summary>The parent document identifier, when supplied.</summary>
    public PatentDocumentIdentifier? ParentDocument = null;

    /// <summary>The child document identifier, when supplied.</summary>
    public PatentDocumentIdentifier? ChildDocument = null;
}

/// <summary>
/// Holds an external image or other resource referenced by full-text XML.
/// </summary>
public class PatentExternalResource
{
    /// <summary>The source resource identifier.</summary>
    public string? Identifier = null;

    /// <summary>The containing figure identifier.</summary>
    public string? FigureIdentifier = null;

    /// <summary>The containing figure number.</summary>
    public string? FigureNumber = null;

    /// <summary>The external file name or source reference.</summary>
    public string? FileName = null;

    /// <summary>The declared resource format.</summary>
    public string? Format = null;

    /// <summary>The declared resource content category.</summary>
    public string? ContentCategory = null;

    /// <summary>The alternative text supplied for the resource.</summary>
    public string? AlternativeText = null;

    /// <summary>The declared resource orientation.</summary>
    public string? Orientation = null;

    /// <summary>The source width including its unit.</summary>
    public string? Width = null;

    /// <summary>The source height including its unit.</summary>
    public string? Height = null;
}

/// <summary>
/// Holds bibliographic data parsed from a grant or published application.
/// </summary>
public class PatentBibliographicData
{
    /// <summary>The publication document identifier.</summary>
    public PatentDocumentIdentifier Publication = new PatentDocumentIdentifier();

    /// <summary>The application document identifier.</summary>
    public PatentDocumentIdentifier Application = new PatentDocumentIdentifier();

    /// <summary>The application category supplied by the source.</summary>
    public string? ApplicationType = null;

    /// <summary>The USPTO application series code.</summary>
    public string? ApplicationSeriesCode = null;

    /// <summary>The invention title.</summary>
    public string? InventionTitle = null;

    /// <summary>The number of claims reported by the source.</summary>
    public int ClaimCount = 0;

    /// <summary>Whether <see cref="ClaimCount"/> was supplied as a valid integer.</summary>
    public bool HasClaimCount = false;

    /// <summary>The applicants, inventors, examiners, assignees, and agents.</summary>
    public PatentParty Parties = new PatentParty();

    /// <summary>The CPC, IPC, USPC, WIPO, and government-interest data.</summary>
    public PatentClassificationData Classifications = new PatentClassificationData();

    /// <summary>The cited patent and non-patent literature records.</summary>
    public PatentCitation[] Citations = new PatentCitation[0];

    /// <summary>The related application and publication records.</summary>
    public PatentRelatedApplication[] RelatedApplications = new PatentRelatedApplication[0];
}

/// <summary>
/// Holds a typed ICE or ST.36 patent grant or published-application document.
/// </summary>
public class PatentFullText
{
    /// <summary>The XML root element name.</summary>
    public string? RootElementName = null;

    /// <summary>The normalized document category, either grant or published application.</summary>
    public string? DocumentCategory = null;

    /// <summary>The root document identifier.</summary>
    public string? RootIdentifier = null;

    /// <summary>The source XML file name.</summary>
    public string? SourceFileName = null;

    /// <summary>The declared DTD version.</summary>
    public string? DtdVersion = null;

    /// <summary>The document language code.</summary>
    public string? LanguageCode = null;

    /// <summary>The document country code.</summary>
    public string? CountryCode = null;

    /// <summary>The source date-produced text.</summary>
    public string? ProducedDateText = null;

    /// <summary>The parsed production date.</summary>
    public global::System.DateTimeOffset ProducedDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ProducedDate"/> was parsed from a complete valid date.</summary>
    public bool HasProducedDate = false;

    /// <summary>The source publication-date text.</summary>
    public string? PublishedDateText = null;

    /// <summary>The parsed publication date.</summary>
    public global::System.DateTimeOffset PublishedDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="PublishedDate"/> was parsed from a complete valid date.</summary>
    public bool HasPublishedDate = false;

    /// <summary>The document bibliographic data.</summary>
    public PatentBibliographicData BibliographicData = new PatentBibliographicData();

    /// <summary>The ordered abstract blocks.</summary>
    public PatentTextBlock[] Abstract = new PatentTextBlock[0];

    /// <summary>The ordered description headings and paragraphs.</summary>
    public PatentTextBlock[] Description = new PatentTextBlock[0];

    /// <summary>The claims in source order.</summary>
    public PatentClaim[] Claims = new PatentClaim[0];

    /// <summary>The external images and other resources in source order.</summary>
    public PatentExternalResource[] ExternalResources = new PatentExternalResource[0];

    /// <summary>The origin and retrieval context of the XML.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();
}
