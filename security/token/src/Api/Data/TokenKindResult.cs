using System;
namespace token;

/// <summary>
/// Contains the outcome of token kind detection.
/// </summary>
public class TokenKindResult
{
    /// <summary>
    /// Indicates whether detection succeeded.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Contains failure details, or <c>null</c> when no error is present.
    /// </summary>
    public TokenError? Error;

    /// <summary>
    /// Contains the detected representation kind.
    /// </summary>
    public TokenKind Kind;

    /// <summary>
    /// Initializes an unsuccessful detection result with an unknown kind.
    /// </summary>
    public TokenKindResult()
    {
        Success = false;
        Error = null;
        Kind = TokenKind.Unknown;
    }
}
