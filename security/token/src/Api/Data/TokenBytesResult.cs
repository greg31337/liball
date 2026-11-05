using System;
namespace token;

/// <summary>
/// Contains the outcome and byte value of a token operation.
/// </summary>
public class TokenBytesResult
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
    /// Contains the operation output, or <c>null</c> when no bytes are available.
    /// </summary>
    public byte[]? Bytes;

    /// <summary>
    /// Initializes an unsuccessful result with no output or error details.
    /// </summary>
    public TokenBytesResult()
    {
        Success = false;
        Error = null;
        Bytes = null;
    }
}
