using System;
namespace token;

/// <summary>
/// Contains the outcome of parsing, verifying, or diagnostically reading compact JWS text.
/// </summary>
public class JwsCompactResult
{
    /// <summary>Indicates whether the requested JWS operation succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when the operation succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the parsed compact JWS, or <c>null</c> when the operation failed.</summary>
    public JwsCompactToken? Token;

    /// <summary>Initializes an unsuccessful result with no details.</summary>
    public JwsCompactResult()
    {
        Success = false;
        Error = null;
        Token = null;
    }
}
