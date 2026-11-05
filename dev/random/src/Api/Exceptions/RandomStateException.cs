using System;

namespace random;

/// <summary>
/// Represents malformed, incompatible, corrupt, or otherwise invalid deterministic state.
/// </summary>
public class RandomStateException : System.Exception
{
    /// <summary>
    /// Creates a deterministic-state error with an optional underlying cause.
    /// </summary>
    /// <param name="message">The message that explains the error.</param>
    /// <param name="innerException">The exception that caused this error, or null when there is no underlying cause.</param>
    public RandomStateException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a deterministic-state error without an underlying cause.
    /// </summary>
    /// <param name="message">The message that explains the error.</param>
    /// <returns>A deterministic-state error with no inner exception.</returns>
    internal static RandomStateException Create(string message)
    {
        return new RandomStateException(message, null);
    }
}
