using System;
using System.Numerics;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Contains the outcome of parsing a JSON Web Key.
/// </summary>
public class JwkResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when parsing succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the parsed key, or <c>null</c> when parsing failed.</summary>
    public TokenKey? Key;

    /// <summary>Initializes an unsuccessful result with no details.</summary>
    public JwkResult()
    {
        Success = false;
        Error = null;
        Key = null;
    }
}
