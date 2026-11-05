using System;
namespace token;

/// <summary>Contains the outcome of parsing or writing a CWT Claims Set.</summary>
public class CwtClaimsResult
{
    /// <summary>Indicates whether the claim set was processed successfully.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Contains the parsed claims, or <c>null</c> on failure.</summary>
    public CwtClaims? Claims;

    /// <summary>Initializes an unsuccessful result.</summary>
    public CwtClaimsResult()
    {
        Success = false;
        Error = null;
        Claims = null;
    }
}
