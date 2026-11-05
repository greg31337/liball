using System;
namespace token;

/// <summary>Provides representation-neutral token detection, creation, inspection, and validation.</summary>
public class UniversalTokenService : IUniversalTokenOperations
{
    /// <summary>Initializes a stateless universal token service.</summary>
    public UniversalTokenService()
    {
    }

    /// <summary>Detects a supported text representation without exposing payload content.</summary>
    public TokenKindResult DetectTextToken(string text)
    {
        return TokenDetector.Detect(text);
    }

    /// <summary>Detects a supported binary representation without exposing payload content.</summary>
    public TokenKindResult DetectBinaryToken(byte[] bytes)
    {
        return TokenDetector.DetectBytes(bytes);
    }

    /// <summary>Safely inspects headers and policy-authorized claims in a text token.</summary>
    public TokenInspectionResult InspectTextToken(
        string text,
        TokenPolicy policy)
    {
        return UniversalToken.Inspect(text, policy);
    }

    /// <summary>Safely inspects headers and policy-authorized claims in a binary token.</summary>
    public TokenInspectionResult InspectBinaryToken(
        byte[] bytes,
        TokenPolicy policy)
    {
        return UniversalToken.InspectBytes(bytes, policy);
    }

    /// <summary>Creates a supported JOSE, COSE, JWT, or CWT representation.</summary>
    public UniversalTokenCreationResult CreateToken(
        UniversalTokenCreationRequest request,
        TokenPolicy policy)
    {
        return UniversalTokenCreator.CreateToken(request, policy);
    }

    /// <summary>Validates a supported text token through its format-specific validator.</summary>
    public TokenValidationResult ValidateTextToken(
        string text,
        ITokenKeyResolver resolver,
        TokenPolicy policy)
    {
        return UniversalToken.Validate(text, resolver, policy);
    }

    /// <summary>Validates a supported binary token through its format-specific validator.</summary>
    public TokenValidationResult ValidateBinaryToken(
        byte[] bytes,
        ITokenKeyResolver resolver,
        TokenPolicy policy)
    {
        return UniversalToken.ValidateBytes(bytes, resolver, policy);
    }
}
