using System;
namespace token;

/// <summary>Provides representation-neutral token inspection and validation.</summary>
public interface IUniversalTokenOperations
{
    /// <summary>Detects a supported text representation without exposing payload content.</summary>
    /// <param name="text">The token text to classify.</param>
    /// <returns>The detected kind, unknown kind, or stable failure details.</returns>
    TokenKindResult DetectTextToken(string text);

    /// <summary>Detects a supported binary representation without exposing payload content.</summary>
    /// <param name="bytes">The token bytes to classify.</param>
    /// <returns>The detected kind, unknown kind, or stable failure details.</returns>
    TokenKindResult DetectBinaryToken(byte[] bytes);

    /// <summary>Safely inspects headers and policy-authorized claims in a text token.</summary>
    /// <param name="text">The encoded token text.</param>
    /// <param name="policy">The inspection and resource controls.</param>
    /// <returns>The unauthenticated inspection metadata, or stable failure details.</returns>
    TokenInspectionResult InspectTextToken(string text, TokenPolicy policy);

    /// <summary>Safely inspects headers and policy-authorized claims in a binary token.</summary>
    /// <param name="bytes">The encoded token bytes.</param>
    /// <param name="policy">The inspection and resource controls.</param>
    /// <returns>The unauthenticated inspection metadata, or stable failure details.</returns>
    TokenInspectionResult InspectBinaryToken(byte[] bytes, TokenPolicy policy);

    /// <summary>Creates a supported JOSE, COSE, JWT, or CWT representation.</summary>
    /// <param name="request">The representation-neutral creation request.</param>
    /// <param name="policy">The algorithm and resource controls.</param>
    /// <returns>The created text or bytes, or stable failure details.</returns>
    UniversalTokenCreationResult CreateToken(
        UniversalTokenCreationRequest request,
        TokenPolicy policy);

    /// <summary>Validates a supported text token through its format-specific validator.</summary>
    /// <param name="text">The encoded token text.</param>
    /// <param name="resolver">The validation key resolver.</param>
    /// <param name="policy">The validation and resource controls.</param>
    /// <returns>Normalized authenticated metadata, or stable failure diagnostics.</returns>
    TokenValidationResult ValidateTextToken(
        string text,
        ITokenKeyResolver resolver,
        TokenPolicy policy);

    /// <summary>Validates a supported binary token through its format-specific validator.</summary>
    /// <param name="bytes">The encoded token bytes.</param>
    /// <param name="resolver">The validation key resolver.</param>
    /// <param name="policy">The validation and resource controls.</param>
    /// <returns>Normalized authenticated metadata, or stable failure diagnostics.</returns>
    TokenValidationResult ValidateBinaryToken(
        byte[] bytes,
        ITokenKeyResolver resolver,
        TokenPolicy policy);
}
