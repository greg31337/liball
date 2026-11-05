using System;
using System.Security.Cryptography;
using System.Text;

namespace token;

/// <summary>Contains the outcome of reading or decrypting general JWE JSON.</summary>
public class JweGeneralResult
{
    /// <summary>Indicates whether the requested operation succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Contains the general JWE, or <c>null</c> on failure.</summary>
    public JweGeneralToken? Token;

    /// <summary>Initializes an unsuccessful result.</summary>
    public JweGeneralResult()
    {
        Success = false;
        Error = null;
        Token = null;
    }
}
