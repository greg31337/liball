namespace icc;

/// <summary>
/// Represents a recognized ICC feature, tag, element, profile generation, or carrier that a throwing operation cannot process.
/// </summary>
/// <remarks>
/// Capability probes and other exception-less operations return <see cref="IccOperationResult"/> with an unsupported-feature status instead.
/// </remarks>
public class IccUnsupportedFeatureException : System.Exception
{
    /// <summary>
    /// Initializes an exception with a description of the unsupported feature.
    /// </summary>
    /// <param name="message">The message that identifies the unsupported feature.</param>
    public IccUnsupportedFeatureException(string message) : base(message)
    {
    }
}
