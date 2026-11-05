namespace uspto;

/// <summary>
/// Describes one representation available for downloading a patent file-wrapper document.
/// </summary>
public class PatentDocumentDownloadAlternative
{
    /// <summary>The source's MIME type or format identifier.</summary>
    public string? MimeTypeIdentifier = null;

    /// <summary>The normalized MIME media type, when recognized.</summary>
    public string? MediaType = null;

    /// <summary>The recognized document representation.</summary>
    public DocumentFormat Format = DocumentFormat.Unknown;

    /// <summary>The credential-free URI for this representation.</summary>
    public global::System.Uri? DownloadUri = null;

    /// <summary>The source filename, when supplied.</summary>
    public string? FileName = null;

    /// <summary>The number of pages in this representation.</summary>
    public int PageCount = 0;

    /// <summary>Whether <see cref="PageCount"/> was supplied.</summary>
    public bool HasPageCount = false;

    /// <summary>The representation size in bytes.</summary>
    public long Size = 0L;

    /// <summary>Whether <see cref="Size"/> was supplied.</summary>
    public bool HasSize = false;

    /// <summary>Unmodeled download fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Describes one document in a patent application's file wrapper.
/// </summary>
public class PatentDocument
{
    /// <summary>The canonical patent application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The source identifier assigned to the document.</summary>
    public string? DocumentIdentifier = null;

    /// <summary>The USPTO document code.</summary>
    public string? Code = null;

    /// <summary>The human-readable document-code description.</summary>
    public string? Description = null;

    /// <summary>The incoming, outgoing, or internal direction category.</summary>
    public string? DirectionCategory = null;

    /// <summary>The official date assigned to the document.</summary>
    public global::System.DateTimeOffset OfficialDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="OfficialDate"/> was supplied.</summary>
    public bool HasOfficialDate = false;

    /// <summary>The date on which the document was filed.</summary>
    public global::System.DateTimeOffset FiledAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FiledAt"/> was supplied.</summary>
    public bool HasFiledAt = false;

    /// <summary>The time at which the source created the document record.</summary>
    public global::System.DateTimeOffset CreatedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="CreatedAt"/> was supplied.</summary>
    public bool HasCreatedAt = false;

    /// <summary>The source's last-modified time for the document record.</summary>
    public global::System.DateTimeOffset LastModifiedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModifiedAt"/> was supplied.</summary>
    public bool HasLastModifiedAt = false;

    /// <summary>The representations available for downloading this document.</summary>
    public PatentDocumentDownloadAlternative[] DownloadAlternatives = new PatentDocumentDownloadAlternative[0];

    /// <summary>Unmodeled document fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds the file-wrapper documents returned for one patent application.
/// </summary>
public class PatentDocumentList
{
    /// <summary>The canonical patent application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The returned documents in source order.</summary>
    public PatentDocument[] Documents = new PatentDocument[0];

    /// <summary>The number of returned documents.</summary>
    public int Count = 0;

    /// <summary>The service request identifier associated with the response.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and authority of the document listing.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>Unmodeled response fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds the grant and pre-grant publication files associated with one patent application.
/// </summary>
public class PatentAssociatedDocumentList
{
    /// <summary>The canonical patent application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The pre-grant publication ZIP and XML metadata returned by the source.</summary>
    public PatentFileMetadata[] PublicationFiles = new PatentFileMetadata[0];

    /// <summary>The patent grant ZIP and XML metadata returned by the source.</summary>
    public PatentFileMetadata[] GrantFiles = new PatentFileMetadata[0];

    /// <summary>The total number of associated file metadata entries.</summary>
    public int Count = 0;

    /// <summary>The service request identifier associated with the response.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and authority of the associated-document listing.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>Unmodeled response and wrapper fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}
