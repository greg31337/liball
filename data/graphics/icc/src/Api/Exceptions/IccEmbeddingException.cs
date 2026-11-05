namespace icc;

/// <summary>
/// Represents a carrier-structure, extraction, insertion, replacement, or removal failure encountered by a throwing embedding operation.
/// </summary>
/// <remarks>
/// Embedding detection and exception-less extraction or mutation operations return <see cref="IccOperationResult"/> for expected failures instead.
/// </remarks>
public class IccEmbeddingException : System.Exception
{
    /// <summary>
    /// Initializes an exception with a description of the embedding failure.
    /// </summary>
    /// <param name="message">The message that describes the embedding failure.</param>
    public IccEmbeddingException(string message) : base(message)
    {
    }
}
