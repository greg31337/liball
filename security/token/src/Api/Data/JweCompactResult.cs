using System;
using System.Security.Cryptography;

namespace token;

/// <summary>Contains the outcome of parsing, decrypting, or diagnostically reading compact JWE text.</summary>
public class JweCompactResult
{
    /// <summary>Indicates whether the requested operation succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Contains the parsed compact JWE, or <c>null</c> on failure.</summary>
    public JweCompactToken? Token;

    /// <summary>Initializes an unsuccessful result.</summary>
    public JweCompactResult()
    {
        Success = false;
        Error = null;
        Token = null;
    }
}
