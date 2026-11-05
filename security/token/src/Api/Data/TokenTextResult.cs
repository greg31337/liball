using System;
namespace token;

/// <summary>
/// Contains the outcome and text value of a token operation.
/// </summary>
public class TokenTextResult
{
    /// <summary>
    /// Indicates whether the operation succeeded.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Contains failure details, or <c>null</c> when no error is present.
    /// </summary>
    public TokenError? Error;

    /// <summary>
    /// Contains the operation output, or <c>null</c> when no text is available.
    /// </summary>
    public string? Text;

    /// <summary>
    /// Initializes an unsuccessful result with no output or error details.
    /// </summary>
    public TokenTextResult()
    {
        Success = false;
        Error = null;
        Text = null;
    }
}
