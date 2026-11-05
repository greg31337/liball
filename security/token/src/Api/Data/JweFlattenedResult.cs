using System;
using System.Security.Cryptography;
using System.Text;

namespace token;

/// <summary>Contains the outcome of reading or decrypting flattened JWE JSON.</summary>
public class JweFlattenedResult
{
    /// <summary>Indicates whether the requested operation succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Contains the flattened JWE, or <c>null</c> on failure.</summary>
    public JweFlattenedToken? Token;

    /// <summary>Initializes an unsuccessful result.</summary>
    public JweFlattenedResult()
    {
        Success = false;
        Error = null;
        Token = null;
    }
}
