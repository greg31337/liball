namespace iso20022;

/// <summary>Pulls one indexed ISO 20022 business message at a time from a bound input stream.</summary>
public interface IMessageStreamReader
{
    /// <summary>Reads the next business message or returns <c>null</c> after the sequence ends.</summary>
    /// <returns>The next indexed read result, including any item-specific error.</returns>
    MessageStreamItem? ReadNextMessage();
}
