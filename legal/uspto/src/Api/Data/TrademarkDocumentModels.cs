namespace uspto;

/// <summary>
/// Holds the type and category labels assigned to a TSDR case document.
/// </summary>
public class TrademarkDocumentType
{
    /// <summary>The document type name.</summary>
    public string? Name = null;

    /// <summary>The document type description.</summary>
    public string? Description = null;

    /// <summary>The document category name.</summary>
    public string? CategoryName = null;

    /// <summary>The document category description.</summary>
    public string? CategoryDescription = null;

    /// <summary>The extended document description.</summary>
    public string? ExtendedDescription = null;

    /// <summary>Unmodeled type fields retained as XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds metadata for one page of a TSDR case document.
/// </summary>
public class TrademarkDocumentPage
{
    /// <summary>The one-based page number.</summary>
    public int PageNumber = 0;

    /// <summary>Whether <see cref="PageNumber"/> was supplied as a valid positive integer.</summary>
    public bool HasPageNumber = false;

    /// <summary>The source page location.</summary>
    public string? Location = null;

    /// <summary>The source hash or digest identifier.</summary>
    public string? HashDigestIdentifier = null;

    /// <summary>The trademark serial number associated with the page.</summary>
    public string? SerialNumber = null;

    /// <summary>The parsed page scan time.</summary>
    public global::System.DateTimeOffset ScanDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ScanDate"/> was parsed successfully.</summary>
    public bool HasScanDate = false;

    /// <summary>The page scan time exactly as supplied.</summary>
    public string? ScanDateText = null;

    /// <summary>The document type name associated with the page.</summary>
    public string? DocumentTypeName = null;

    /// <summary>The credential-free absolute CMS URI, when supplied.</summary>
    public global::System.Uri? CmsUri = null;

    /// <summary>Unmodeled page fields retained as XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds parsed metadata for one TSDR case document.
/// </summary>
public class TrademarkDocument
{
    /// <summary>The identifier supplied by the caller for the trademark case.</summary>
    public string? RequestedCaseIdentifier = null;

    /// <summary>The canonical trademark case identifier used in the request.</summary>
    public string? CanonicalCaseIdentifier = null;

    /// <summary>The auto-generated TSDR document identifier.</summary>
    public string? DocumentIdentifier = null;

    /// <summary>The source serial or reference number.</summary>
    public string? SerialReferenceNumber = null;

    /// <summary>The document type and category labels.</summary>
    public TrademarkDocumentType Type = new TrademarkDocumentType();

    /// <summary>The parsed document mail time.</summary>
    public global::System.DateTimeOffset MailDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="MailDate"/> was parsed successfully.</summary>
    public bool HasMailDate = false;

    /// <summary>The document mail time exactly as supplied.</summary>
    public string? MailDateText = null;

    /// <summary>The parsed document scan time.</summary>
    public global::System.DateTimeOffset ScanDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ScanDate"/> was parsed successfully.</summary>
    public bool HasScanDate = false;

    /// <summary>The document scan time exactly as supplied.</summary>
    public string? ScanDateText = null;

    /// <summary>The parsed source timestamp.</summary>
    public global::System.DateTimeOffset Timestamp = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="Timestamp"/> was parsed successfully.</summary>
    public bool HasTimestamp = false;

    /// <summary>The source timestamp exactly as supplied.</summary>
    public string? TimestampText = null;

    /// <summary>The source batch identifier.</summary>
    public string? BatchIdentifier = null;

    /// <summary>The source system label.</summary>
    public string? Source = null;

    /// <summary>The pages reported for the document.</summary>
    public TrademarkDocumentPage[] Pages = new TrademarkDocumentPage[0];

    /// <summary>The document type name supplied outside the structured type.</summary>
    public string? DocumentTypeName = null;

    /// <summary>The source URL paths associated with the document.</summary>
    public string[] UrlPaths = new string[0];

    /// <summary>The origin and retrieval context of the document metadata.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>The returned representation of <see cref="RawResponse"/>.</summary>
    public DocumentFormat RawFormat = DocumentFormat.Unknown;

    /// <summary>A bounded retained raw XML response, when requested for a single document.</summary>
    public string? RawResponse = null;

    /// <summary>Unmodeled document fields retained as XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds the parsed TSDR document listing for one trademark case.
/// </summary>
public class TrademarkDocumentCollection
{
    /// <summary>The identifier supplied by the caller.</summary>
    public string? RequestedIdentifier = null;

    /// <summary>The canonical trademark identifier used in the request.</summary>
    public string? CanonicalIdentifier = null;

    /// <summary>The kind of canonical trademark identifier.</summary>
    public TrademarkIdentifierKind IdentifierKind = TrademarkIdentifierKind.Unknown;

    /// <summary>The documents returned in source order.</summary>
    public TrademarkDocument[] Documents = new TrademarkDocument[0];

    /// <summary>The origin and retrieval context of the document listing.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>The returned representation of <see cref="RawResponse"/>.</summary>
    public DocumentFormat RawFormat = DocumentFormat.Unknown;

    /// <summary>A bounded retained raw XML response, when requested.</summary>
    public string? RawResponse = null;

    /// <summary>Whether the complete document listing was processed.</summary>
    public bool IsComplete = false;

    /// <summary>Unmodeled listing fields retained as XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}
