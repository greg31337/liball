namespace icc;

/// <summary>
/// Represents malformed, truncated, or structurally inconsistent profile input encountered by a throwing read or parse operation.
/// </summary>
/// <remarks>
/// Exception-less profile probes and read attempts return <see cref="IccOperationResult"/> instead.
/// </remarks>
public class IccProfileFormatException : System.Exception
{
    /// <summary>
    /// Initializes an exception with a description of the invalid profile input.
    /// </summary>
    /// <param name="message">The message that describes the format failure.</param>
    public IccProfileFormatException(string message) : base(message)
    {
    }
}
