namespace uspto;

/// <summary>
/// Identifies the direction of a citation relative to its source document.
/// </summary>
public enum CitationDirection
{
    /// <summary>The citation direction is not known.</summary>
    Unknown = 0,

    /// <summary>The source document cites the referenced document or literature.</summary>
    Outgoing = 1,

    /// <summary>The source document is cited by the referenced document.</summary>
    Incoming = 2
}
