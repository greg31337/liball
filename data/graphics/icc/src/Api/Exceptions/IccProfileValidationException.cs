namespace icc;

/// <summary>
/// Represents a validation failure that prevents a throwing build, mutation, link, or write operation from producing a conforming profile.
/// </summary>
/// <remarks>
/// Validation and conformance-report operations return their report or an <see cref="IccOperationResult"/> for expected findings instead.
/// </remarks>
public class IccProfileValidationException : System.Exception
{
    /// <summary>
    /// Initializes an exception with a description of the validation failure.
    /// </summary>
    /// <param name="message">The message that describes the validation failure.</param>
    public IccProfileValidationException(string message) : base(message)
    {
    }
}
