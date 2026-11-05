using System;
namespace token;

/// <summary>Contains CWT claims read from a COSE header parameter.</summary>
public class CoseCwtClaimsResult
{
    /// <summary>Indicates whether the header parameter was processed successfully.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Indicates whether the CWT Claims header parameter is present.</summary>
    public bool Present;

    /// <summary>Indicates whether the parameter occurs in the protected header bucket.</summary>
    public bool Protected;

    /// <summary>Contains the parsed claims, or <c>null</c> when absent or invalid.</summary>
    public CwtClaims? Claims;

    /// <summary>Initializes an unsuccessful result with no claims.</summary>
    public CoseCwtClaimsResult()
    {
        Success = false;
        Error = null;
        Present = false;
        Protected = false;
        Claims = null;
    }
}
