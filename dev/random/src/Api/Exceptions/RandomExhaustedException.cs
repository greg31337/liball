using System;

namespace random;

/// <summary>
/// Represents exhaustion of an engine period, counter space, sequence, or explicit attempt budget.
/// </summary>
public class RandomExhaustedException : System.Exception
{
    /// <summary>
    /// Creates an exhaustion error with an optional underlying cause.
    /// </summary>
    /// <param name="message">The message that explains the error.</param>
    /// <param name="innerException">The exception that caused this error, or null when there is no underlying cause.</param>
    public RandomExhaustedException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates an exhaustion error without an underlying cause.
    /// </summary>
    /// <param name="message">The message that explains the error.</param>
    /// <returns>An exhaustion error with no inner exception.</returns>
    internal static RandomExhaustedException Create(string message)
    {
        return new RandomExhaustedException(message, null);
    }
}
