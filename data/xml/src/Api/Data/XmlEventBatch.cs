namespace xml;

/// <summary>
/// Carries a bounded segment of normalized XML events and parser state.
/// </summary>
public class XmlEventBatch
{
    /// <summary>
    /// The event storage.
    /// </summary>
    public XmlEvent[]? Events;
    /// <summary>
    /// The first event index in the batch.
    /// </summary>
    public int Offset;
    /// <summary>
    /// The number of events in the batch.
    /// </summary>
    public int Count;
    /// <summary>
    /// The parser status after producing the batch.
    /// </summary>
    public XmlParserStatus Status;
    /// <summary>
    /// Whether the batch is the final batch.
    /// </summary>
    public bool IsFinal;
    /// <summary>
    /// The reader state associated with the batch.
    /// </summary>
    public XmlReaderState? ReaderState;
}

