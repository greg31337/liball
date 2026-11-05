using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace token;

/// <summary>
/// Contains the outcome of parsing or validating a JWT claim set.
/// </summary>
public class JwtClaimsResult
{
    /// <summary>Indicates whether the claim set was processed successfully.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when processing succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the parsed claims, or <c>null</c> when processing failed.</summary>
    public JwtClaims? Claims;

    /// <summary>Initializes an unsuccessful result with no details.</summary>
    public JwtClaimsResult()
    {
        Success = false;
        Error = null;
        Claims = null;
    }
}
