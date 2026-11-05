namespace uspto;

/// <summary>
/// Holds the filing and content metadata for one PTAB document.
/// </summary>
public class PtabDocument
{
    /// <summary>The service document identifier.</summary>
    public string? DocumentIdentifier = null;

    /// <summary>The document number within the trial record.</summary>
    public int DocumentNumber = 0;

    /// <summary>Whether <see cref="DocumentNumber"/> was supplied.</summary>
    public bool HasDocumentNumber = false;

    /// <summary>The source document name.</summary>
    public string? Name = null;

    /// <summary>The document category.</summary>
    public string? Category = null;

    /// <summary>The filing party category.</summary>
    public string? FilingPartyCategory = null;

    /// <summary>The document title.</summary>
    public string? Title = null;

    /// <summary>The document type description.</summary>
    public string? TypeDescription = null;

    /// <summary>The document filing date.</summary>
    public global::System.DateTimeOffset FilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FilingDate"/> was supplied.</summary>
    public bool HasFilingDate = false;

    /// <summary>The document size in bytes.</summary>
    public long Size = 0L;

    /// <summary>Whether <see cref="Size"/> was supplied.</summary>
    public bool HasSize = false;

    /// <summary>The source MIME type identifier.</summary>
    public string? MimeTypeIdentifier = null;

    /// <summary>The normalized MIME media type, when recognized.</summary>
    public string? MediaType = null;

    /// <summary>The normalized document format.</summary>
    public DocumentFormat Format = DocumentFormat.Unknown;

    /// <summary>The credential-free document download URI.</summary>
    public global::System.Uri? DownloadUri = null;

    /// <summary>The source-provided OCR text.</summary>
    public string? OcrText = null;

    /// <summary>The origin and authority of this document.</summary>
    public PtabDataProvenance Provenance = new PtabDataProvenance();

    /// <summary>Unmodeled document fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one document record together with its PTAB trial context.
/// </summary>
public class PtabTrialDocument
{
    /// <summary>The trial-document record category.</summary>
    public string? RecordCategory = null;

    /// <summary>The PTAB trial number.</summary>
    public string? TrialNumber = null;

    /// <summary>The trial type code.</summary>
    public string? TrialTypeCode = null;

    /// <summary>The source-reported record modification time.</summary>
    public global::System.DateTimeOffset LastModifiedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModifiedAt"/> was supplied.</summary>
    public bool HasLastModifiedAt = false;

    /// <summary>The trial lifecycle metadata.</summary>
    public PtabTrialMetadata Metadata = new PtabTrialMetadata();

    /// <summary>The challenged patent owner data.</summary>
    public PtabPatentOwner PatentOwner = new PtabPatentOwner();

    /// <summary>The regular petitioner data.</summary>
    public PtabPetitioner RegularPetitioner = new PtabPetitioner();

    /// <summary>The respondent data.</summary>
    public PtabRespondent Respondent = new PtabRespondent();

    /// <summary>The derivation petitioner data.</summary>
    public PtabPetitioner DerivationPetitioner = new PtabPetitioner();

    /// <summary>The filed document data.</summary>
    public PtabDocument Document = new PtabDocument();

    /// <summary>The decision data associated with the document, when present.</summary>
    public PtabDecision Decision = new PtabDecision();

    /// <summary>The origin and authority of this trial document record.</summary>
    public PtabDataProvenance Provenance = new PtabDataProvenance();

    /// <summary>Unmodeled trial document fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one page or bounded aggregate of PTAB trial documents.
/// </summary>
public class PtabTrialDocumentResponse
{
    /// <summary>The search request associated with the response.</summary>
    public SearchRequest Request = new SearchRequest();

    /// <summary>The returned trial documents.</summary>
    public PtabTrialDocument[] Documents = new PtabTrialDocument[0];

    /// <summary>The returned facet data.</summary>
    public PtabFacet[] Facets = new PtabFacet[0];

    /// <summary>The number of documents held in this response.</summary>
    public int Count = 0;

    /// <summary>The service-reported total number of matching documents.</summary>
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
