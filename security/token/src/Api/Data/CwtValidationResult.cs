using System;
namespace token;

/// <summary>Contains an authenticated and claim-validated CBOR Web Token.</summary>
public class CwtValidationResult
{
    /// <summary>Indicates whether every COSE layer and the CWT Claims Set are valid.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Contains the authenticated CWT Claims Set.</summary>
    public CwtClaims? Claims;

    /// <summary>Contains protection kinds in outermost-to-innermost order.</summary>
    public CoseMessageKind[] ProtectionKinds;

    /// <summary>Contains authenticated message-level headers in outermost-to-innermost order.</summary>
    public CoseHeader[] Headers;

    /// <summary>Indicates whether the complete token used CBOR tag 61.</summary>
    public bool CwtTagged;

    /// <summary>Indicates whether the token contained more than one COSE protection layer.</summary>
    public bool Nested;

    /// <summary>Initializes an unsuccessful validation result.</summary>
    public CwtValidationResult()
    {
        Success = false;
        Error = null;
        Claims = null;
        ProtectionKinds = Array.Empty<CoseMessageKind>();
        Headers = Array.Empty<CoseHeader>();
        CwtTagged = false;
        Nested = false;
    }
}
