using System;
using System.Text;

namespace token;

/// <summary>Contains the outcome of reading or verifying a general JWS JSON serialization.</summary>
public class JwsGeneralResult
{
    /// <summary>Indicates whether the requested operation succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Contains the general JWS, or <c>null</c> on failure.</summary>
    public JwsGeneralToken? Token;

    /// <summary>Initializes an unsuccessful result.</summary>
    public JwsGeneralResult()
    {
        Success = false;
        Error = null;
        Token = null;
    }
}
