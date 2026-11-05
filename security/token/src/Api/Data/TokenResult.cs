using System;
namespace token;

/// <summary>
/// Contains the outcome of a token operation that does not return a value.
/// </summary>
public class TokenResult
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
    /// Initializes an unsuccessful result with no error details.
    /// </summary>
    public TokenResult()
    {
        Success = false;
        Error = null;
    }
}
