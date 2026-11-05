using System;
namespace token;

/// <summary>
/// Describes a token operation failure.
/// </summary>
public class TokenError
{
    /// <summary>
    /// Gets the stable, machine-readable error code.
    /// </summary>
    public string Code;

    /// <summary>
    /// Gets the human-readable error description.
    /// </summary>
    public string Message;

    /// <summary>
    /// Gets the zero-based input position associated with the error, or <c>-1</c> when no position is available.
    /// </summary>
    public int Position;

    /// <summary>
    /// Initializes an error with a code, message, and input position.
    /// </summary>
    /// <param name="code">The stable, machine-readable error code.</param>
    /// <param name="message">The human-readable error description.</param>
    /// <param name="position">The zero-based input position, or <c>-1</c> when no position is available.</param>
    public TokenError(string code, string message, int position)
    {
        Code = code;
        Message = message;
        Position = position;
    }

    /// <summary>
    /// Creates an empty error.
    /// </summary>
    /// <returns>An error with empty text and no input position.</returns>
    internal static TokenError CreateEmpty()
    {
        return new TokenError(string.Empty, string.Empty, -1);
    }
}
