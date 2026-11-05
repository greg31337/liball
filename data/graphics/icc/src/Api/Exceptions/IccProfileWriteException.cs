namespace icc;

/// <summary>
/// Represents a serialization, output-layout, or destination failure encountered by a throwing profile preflight or write operation.
/// </summary>
/// <remarks>
/// Exception-less preflight and write attempts return <see cref="IccOperationResult"/> for expected unsuccessful outcomes instead.
/// </remarks>
public class IccProfileWriteException : System.Exception
{
    /// <summary>
    /// Initializes an exception with a description of the profile write failure.
    /// </summary>
    /// <param name="message">The message that describes the write failure.</param>
    public IccProfileWriteException(string message) : base(message)
    {
    }
}
