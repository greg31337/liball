namespace uspto;

/// <summary>
/// Holds the update timestamps reported for one TSDR case component set.
/// </summary>
public class TrademarkCaseUpdateInfo
{
    /// <summary>The source update entry name.</summary>
    public string? Name = null;

    /// <summary>The source update entry value.</summary>
    public string? Value = null;

    /// <summary>The parsed overall case update time.</summary>
    public global::System.DateTimeOffset LastModifiedDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModifiedDate"/> was parsed successfully.</summary>
    public bool HasLastModifiedDate = false;

    /// <summary>The overall case update time exactly as supplied.</summary>
    public string? LastModifiedDateText = null;

    /// <summary>The parsed case-status update time.</summary>
    public global::System.DateTimeOffset StatusLastModifiedDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="StatusLastModifiedDate"/> was parsed successfully.</summary>
    public bool HasStatusLastModifiedDate = false;

    /// <summary>The case-status update time exactly as supplied.</summary>
    public string? StatusLastModifiedDateText = null;

    /// <summary>The parsed prosecution-history update time.</summary>
    public global::System.DateTimeOffset ProsecutionLastModifiedDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ProsecutionLastModifiedDate"/> was parsed successfully.</summary>
    public bool HasProsecutionLastModifiedDate = false;

    /// <summary>The prosecution-history update time exactly as supplied.</summary>
    public string? ProsecutionLastModifiedDateText = null;

    /// <summary>The parsed case-document update time.</summary>
    public global::System.DateTimeOffset DocumentsLastModifiedDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="DocumentsLastModifiedDate"/> was parsed successfully.</summary>
    public bool HasDocumentsLastModifiedDate = false;

    /// <summary>The case-document update time exactly as supplied.</summary>
    public string? DocumentsLastModifiedDateText = null;

    /// <summary>Unmodeled update fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds the last-update response for one trademark serial number.
/// </summary>
public class TrademarkLastUpdateResult
{
    /// <summary>The identifier supplied by the caller.</summary>
    public string? RequestedIdentifier = null;

    /// <summary>The canonical serial-number identifier used in the request.</summary>
    public string? CanonicalIdentifier = null;

    /// <summary>The update entries returned in source order.</summary>
    public TrademarkCaseUpdateInfo[] Updates = new TrademarkCaseUpdateInfo[0];

    /// <summary>The origin and retrieval context of the update information.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>The returned JSON or XML representation.</summary>
    public DocumentFormat RawFormat = DocumentFormat.Unknown;

    /// <summary>A bounded retained raw response, when requested.</summary>
    public string? RawResponse = null;

    /// <summary>Whether the complete update response was processed.</summary>
    public bool IsComplete = false;

    /// <summary>Unmodeled response fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds a bounded multi-case TSDR status transaction.
/// </summary>
public class TrademarkMultiStatusResult
{
    /// <summary>The identifiers supplied by the caller.</summary>
    public string[] RequestedIdentifiers = new string[0];

    /// <summary>The normalized identifiers sent to TSDR.</summary>
    public string[] CanonicalIdentifiers = new string[0];

    /// <summary>The homogeneous identifier kind used by the transaction route.</summary>
    public TrademarkIdentifierKind IdentifierKind = TrademarkIdentifierKind.Unknown;

    /// <summary>The successfully parsed case-status records.</summary>
    public TrademarkCaseStatus[] Cases = new TrademarkCaseStatus[0];

    /// <summary>The case identifiers reported as missed by TSDR.</summary>
    public string[] MissedIdentifiers = new string[0];

    /// <summary>Whether TSDR reports that the requested transaction was oversized.</summary>
    public bool IsOversized = false;

    /// <summary>Whether the oversized indicator was supplied.</summary>
    public bool HasIsOversized = false;

    /// <summary>The transaction size reported by TSDR.</summary>
    public int Size = 0;

    /// <summary>Whether <see cref="Size"/> was supplied.</summary>
    public bool HasSize = false;

    /// <summary>The origin and retrieval context of the transaction.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>A bounded retained raw JSON response, when requested.</summary>
    public string? RawResponse = null;

    /// <summary>Whether the complete transaction response was processed.</summary>
    public bool IsComplete = false;

    /// <summary>Unmodeled transaction fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}
