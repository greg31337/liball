using System;

namespace random;

/// <summary>
/// Represents a secure generator that must be reseeded before generation can continue.
/// </summary>
public class RandomReseedRequiredException : System.Exception
{
    /// <summary>
    /// Creates a reseed-required error with an optional underlying cause.
    /// </summary>
    /// <param name="message">The message that explains the error.</param>
    /// <param name="innerException">The exception that caused this error, or null when there is no underlying cause.</param>
    public RandomReseedRequiredException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a reseed-required error without an underlying cause.
    /// </summary>
    /// <param name="message">The message that explains the error.</param>
    /// <returns>A reseed-required error with no inner exception.</returns>
    internal static RandomReseedRequiredException Create(string message)
    {
        return new RandomReseedRequiredException(message, null);
    }
}
