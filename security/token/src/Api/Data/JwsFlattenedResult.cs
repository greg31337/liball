using System;
using System.Text;

namespace token;

/// <summary>Contains the outcome of a flattened JWS JSON read or verification.</summary>
public class JwsFlattenedResult
{
    /// <summary>Indicates whether the requested operation succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Contains the flattened JWS, or <c>null</c> on failure.</summary>
    public JwsFlattenedToken? Token;

    /// <summary>Initializes an unsuccessful result.</summary>
    public JwsFlattenedResult()
    {
        Success = false;
        Error = null;
        Token = null;
    }
}
