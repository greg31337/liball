using System;

namespace fake;

/// <summary>
/// Represents a failure that occurs while fake data is being generated.
/// </summary>
/// <remarks>
/// Invalid public arguments use the appropriate built-in argument exception.
/// This exception is used for generation failures such as bounded exhaustion.
/// </remarks>
public class FakeException : System.Exception
{
    /// <summary>
    /// Initializes an exception with a specific failure message and cause.
    /// </summary>
    /// <param name="message">The message that describes the failure.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public FakeException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates an exception with the library's default failure message.
    /// </summary>
    /// <returns>A new fake-data generation exception.</returns>
    internal static FakeException Create()
    {
        return new FakeException("Fake data generation failed.", null);
    }

    /// <summary>
    /// Creates an exception with a specific failure message.
    /// </summary>
    /// <param name="message">The message that describes the failure.</param>
    /// <returns>A new fake-data generation exception.</returns>
    internal static FakeException Create(string message)
    {
        return new FakeException(message, null);
    }

    /// <summary>
    /// Creates an exception with a specific failure message and cause.
    /// </summary>
    /// <param name="message">The message that describes the failure.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    /// <returns>A new fake-data generation exception.</returns>
    internal static FakeException Create(string message, Exception innerException)
    {
        return new FakeException(message, innerException);
    }
}
