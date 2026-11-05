namespace icc;

/// <summary>
/// Represents a failure to compile or execute a requested colour transform through a throwing transform operation.
/// </summary>
/// <remarks>
/// Transform capability probes return <see cref="IccOperationResult"/> when incompatibility or an unavailable transform is an expected outcome.
/// </remarks>
public class IccTransformException : System.Exception
{
    /// <summary>
    /// Initializes an exception with a description of the transform failure.
    /// </summary>
    /// <param name="message">The message that describes the transform failure.</param>
    public IccTransformException(string message) : base(message)
    {
    }
}
