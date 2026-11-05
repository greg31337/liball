using System;

namespace random;

/// <summary>
/// Represents an invalid public API argument, parameter combination, or destination shape.
/// </summary>
/// <remarks>
/// Argument validation occurs before random state or caller-owned destinations are changed.
/// </remarks>
public class RandomArgumentException : System.Exception
{
    string argumentName;

    /// <summary>
    /// Creates an argument error with the name of the invalid argument and an optional underlying cause.
    /// </summary>
    /// <param name="argumentName">The public argument name associated with the error.</param>
    /// <param name="message">The message that explains the error.</param>
    /// <param name="innerException">The exception that caused this error, or null when there is no underlying cause.</param>
    public RandomArgumentException(string argumentName, string message, Exception? innerException) : base(message, innerException)
    {
        this.argumentName = argumentName;
    }

    /// <summary>
    /// Creates an argument error without an underlying cause.
    /// </summary>
    /// <param name="argumentName">The public argument name associated with the error.</param>
    /// <param name="message">The message that explains the error.</param>
    /// <returns>An argument error with no inner exception.</returns>
    internal static RandomArgumentException Create(string argumentName, string message)
    {
        return new RandomArgumentException(argumentName, message, null);
    }

    /// <summary>
    /// Returns the public argument name associated with the error.
    /// </summary>
    /// <returns>The argument name supplied when the exception was created.</returns>
    public string ArgumentName()
    {
        return argumentName;
    }
}
