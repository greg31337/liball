using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace token;

/// <summary>
/// Contains parsed SD-JWT components and, after validation, the processed claims.
/// </summary>
public class SelectiveDisclosureJwtResult
{
    /// <summary>Indicates whether parsing or validation succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when processing succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the detected token representation.</summary>
    public TokenKind Kind;

    /// <summary>Contains the compact Issuer-signed JWT.</summary>
    public string? IssuerSignedJwt;

    /// <summary>Contains the parsed Disclosures in presentation order.</summary>
    public SelectiveDisclosure[]? Disclosures;

    /// <summary>Contains the optional Key Binding JWT, or <c>null</c>.</summary>
    public string? KeyBindingJwt;

    /// <summary>Contains the selected Disclosure hash algorithm after validation.</summary>
    public string? HashAlgorithm;

    /// <summary>Contains Issuer JWT authentication metadata after validation, or <c>null</c>.</summary>
    public JwtValidationResult? IssuerJwt;

    /// <summary>Contains the processed claims after successful digest verification, or <c>null</c>.</summary>
    public JwtClaims? Claims;

    /// <summary>Indicates whether every supplied Disclosure digest was verified.</summary>
    public bool DigestsVerified;

    /// <summary>Contains authenticated Key Binding JWT metadata, or <c>null</c> when not required.</summary>
    public JwtValidationResult? KeyBindingJwtValidation;

    /// <summary>Indicates whether RFC 9901 holder key binding was validated.</summary>
    public bool KeyBindingValidated;

    /// <summary>Initializes an unsuccessful, empty SD-JWT result.</summary>
    public SelectiveDisclosureJwtResult()
    {
        Success = false;
        Error = null;
        Kind = TokenKind.Unknown;
        IssuerSignedJwt = null;
        Disclosures = null;
        KeyBindingJwt = null;
        HashAlgorithm = null;
        IssuerJwt = null;
        Claims = null;
        DigestsVerified = false;
        KeyBindingJwtValidation = null;
        KeyBindingValidated = false;
    }
}
