namespace iso20022;

/// <summary>Represents one indexed result returned from a message stream.</summary>
public class MessageStreamItem
{
    /// <summary>The zero-based item index.</summary>
    public long Index;
    /// <summary>The read outcome for the item.</summary>
    public MessageReadResult Result = new MessageReadResult();
    /// <summary>The stable lightweight parent message context for a selected repeating block.</summary>
    public MessageDocument? Context;
    /// <summary>The exact qualified source path of a selected repeating block.</summary>
    public XmlPathData Path = new XmlPathData();
    /// <summary>The location at which the item began.</summary>
    public SourceLocation Location = new SourceLocation();
}
