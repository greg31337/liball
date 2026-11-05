namespace iso20022;

/// <summary>Writes one ISO 20022 business message at a time into a bound sequence envelope.</summary>
public interface IMessageStreamWriter
{
    /// <summary>Writes the next message without retaining its model.</summary>
    /// <param name="document">The next generic message document.</param>
    /// <returns>The outcome and number of bytes emitted by this write.</returns>
    MessageWriteResult WriteNextMessage(MessageDocument? document);
    /// <summary>Finalizes the wrapper and verifies or patches its count and control total.</summary>
    /// <returns>The finalization outcome and complete output byte count when known.</returns>
    MessageWriteResult CompleteWriting();
}
