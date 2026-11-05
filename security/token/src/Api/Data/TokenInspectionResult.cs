using System;
namespace token;

/// <summary>Contains the result of safe, unauthenticated token header inspection.</summary>
public class TokenInspectionResult
{
    /// <summary>Indicates whether a supported token was inspected successfully.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Identifies the inspected token representation.</summary>
    public TokenKind Kind;

    /// <summary>Contains headers in serialization order.</summary>
    public TokenHeaderInspection[] Headers;

    /// <summary>Indicates whether the returned headers were authenticated.</summary>
    /// <remarks>Header-only inspection never authenticates headers, so this field is false.</remarks>
    public bool HeadersAuthenticated;

    /// <summary>Contains inspected JWT claims, or <c>null</c> when unavailable or not requested.</summary>
    public JwtClaims? JwtClaims;

    /// <summary>Contains inspected CWT claims, or <c>null</c> when unavailable or not requested.</summary>
    public CwtClaims? CwtClaims;

    /// <summary>Indicates whether one of the claim-set fields contains decoded claims.</summary>
    public bool ClaimsAvailable;

    /// <summary>Indicates whether the returned claims were cryptographically authenticated.</summary>
    /// <remarks>Inspection never authenticates claims, so this field is false.</remarks>
    public bool ClaimsAuthenticated;

    /// <summary>Contains the reason opted-in claims inspection could not return claims, or <c>null</c>.</summary>
    public TokenError? ClaimsError;

    /// <summary>Initializes an unsuccessful result with no headers.</summary>
    public TokenInspectionResult()
    {
        Success = false;
        Error = null;
        Kind = TokenKind.Unknown;
        Headers = Array.Empty<TokenHeaderInspection>();
        HeadersAuthenticated = false;
        JwtClaims = null;
        CwtClaims = null;
        ClaimsAvailable = false;
        ClaimsAuthenticated = false;
        ClaimsError = null;
    }
}
