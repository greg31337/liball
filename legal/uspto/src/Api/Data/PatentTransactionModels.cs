namespace uspto;

/// <summary>
/// Describes one recorded event in a patent application's transaction history.
/// </summary>
public class PatentTransaction
{
    /// <summary>The event sequence number assigned by the source.</summary>
    public int EventSequenceNumber = 0;

    /// <summary>Whether <see cref="EventSequenceNumber"/> was supplied.</summary>
    public bool HasEventSequenceNumber = false;

    /// <summary>The date on which the patent case action was recorded.</summary>
    public global::System.DateTimeOffset EventDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="EventDate"/> was supplied.</summary>
    public bool HasEventDate = false;

    /// <summary>The source code identifying the event.</summary>
    public string? EventCode = null;

    /// <summary>The human-readable description of the event.</summary>
    public string? Description = null;

    /// <summary>The system or data source that supplied the event.</summary>
    public string? Source = null;

    /// <summary>The source's last-modified time for this event.</summary>
    public global::System.DateTimeOffset LastModifiedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModifiedAt"/> was supplied.</summary>
    public bool HasLastModifiedAt = false;

    /// <summary>Unmodeled event fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds the transaction history returned for one patent application.
/// </summary>
public class PatentTransactionHistory
{
    /// <summary>The canonical patent application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The recorded application events in source order.</summary>
    public PatentTransaction[] Events = new PatentTransaction[0];

    /// <summary>The number of recorded events.</summary>
    public int Count = 0;

    /// <summary>The source's last-modified or ingestion time for the complete history.</summary>
    public global::System.DateTimeOffset LastModifiedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModifiedAt"/> was supplied.</summary>
    public bool HasLastModifiedAt = false;

    /// <summary>The service request identifier associated with the response.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and authority of the transaction history.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>Unmodeled wrapper sections retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownSections = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}
