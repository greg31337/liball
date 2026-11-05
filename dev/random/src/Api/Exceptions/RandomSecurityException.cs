using System;

namespace random;

/// <summary>
/// Represents rejection of an operation or input by the library's security policy.
/// </summary>
public class RandomSecurityException : System.Exception
{
    /// <summary>
    /// Creates a security-policy error with an optional underlying cause.
    /// </summary>
    /// <param name="message">The message that explains the error.</param>
    /// <param name="innerException">The exception that caused this error, or null when there is no underlying cause.</param>
    public RandomSecurityException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a security-policy error without an underlying cause.
    /// </summary>
    /// <param name="message">The message that explains the error.</param>
    /// <returns>A security-policy error with no inner exception.</returns>
    internal static RandomSecurityException Create(string message)
    {
        return new RandomSecurityException(message, null);
    }
}
