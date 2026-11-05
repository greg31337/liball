using System;

namespace random;

/// <summary>
/// Represents an operation that the selected random-number algorithm does not support.
/// </summary>
public class RandomCapabilityException : System.Exception
{
    RandomAlgorithm algorithm;
    RandomCapability capability;

    /// <summary>
    /// Creates an unsupported-capability error with an optional underlying cause.
    /// </summary>
    /// <param name="algorithm">The algorithm on which the operation was requested.</param>
    /// <param name="capability">The unsupported capability.</param>
    /// <param name="message">The message that explains the error.</param>
    /// <param name="innerException">The exception that caused this error, or null when there is no underlying cause.</param>
    public RandomCapabilityException(RandomAlgorithm algorithm, RandomCapability capability, string message, Exception? innerException) : base(message, innerException)
    {
        this.algorithm = algorithm;
        this.capability = capability;
    }

    /// <summary>
    /// Creates an unsupported-capability error without an underlying cause.
    /// </summary>
    /// <param name="algorithm">The algorithm on which the operation was requested.</param>
    /// <param name="capability">The unsupported capability.</param>
    /// <param name="message">The message that explains the error.</param>
    /// <returns>An unsupported-capability error with no inner exception.</returns>
    internal static RandomCapabilityException Create(RandomAlgorithm algorithm, RandomCapability capability, string message)
    {
        return new RandomCapabilityException(algorithm, capability, message, null);
    }

    /// <summary>
    /// Returns the algorithm on which the operation was requested.
    /// </summary>
    /// <returns>The selected algorithm.</returns>
    public RandomAlgorithm Algorithm()
    {
        return algorithm;
    }

    /// <summary>
    /// Returns the unsupported capability.
    /// </summary>
    /// <returns>The requested capability.</returns>
    public RandomCapability Capability()
    {
        return capability;
    }
}
