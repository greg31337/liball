using System;
namespace token;

/// <summary>
/// Provides the high-level JSON Web Token creation facade.
/// </summary>
internal static class JsonWebToken
{
    /// <summary>Validates a signed compact JWT using a key resolver.</summary>
    /// <param name="token">The compact JWT text.</param>
    /// <param name="resolver">The resolver that selects a verification key.</param>
    /// <param name="policy">The JWT validation policy.</param>
    /// <returns>A result containing authenticated claims and header metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwtValidationResult Validate(
        string token,
        ITokenKeyResolver resolver,
        JwtValidationPolicy policy)
    {
        return JwtValidator.Validate(token, resolver, policy);
    }

    /// <summary>Validates an encrypted compact JWT using a key resolver.</summary>
    /// <param name="token">The compact JWT text.</param>
    /// <param name="resolver">The resolver that selects decryption and nested verification keys.</param>
    /// <param name="policy">The JWT validation policy.</param>
    /// <returns>A result containing authenticated claims and header metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwtValidationResult ValidateEncrypted(
        string token,
        ITokenKeyResolver resolver,
        JwtValidationPolicy policy)
    {
        return JwtValidator.ValidateEncrypted(token, resolver, policy);
    }

    /// <summary>Validates a signed compact JWT using a key resolver.</summary>
    /// <param name="token">The compact JWT text.</param>
    /// <param name="resolver">The resolver that selects a verification key.</param>
    /// <param name="policy">The JWT validation policy.</param>
    /// <returns>A result containing authenticated claims and header metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwtValidationResult ValidateSigned(
        string token,
        ITokenKeyResolver resolver,
        JwtValidationPolicy policy)
    {
        return JwtValidator.ValidateSigned(token, resolver, policy);
    }

    /// <summary>
    /// Creates a signed compact JWT and encrypts that complete token as a compact JWE.
    /// </summary>
    /// <param name="claims">The claim set to sign and encrypt.</param>
    /// <param name="signingKey">The key used for the inner JWS.</param>
    /// <param name="encryptionKey">The key used for the outer JWE.</param>
    /// <param name="contentEncryptionAlgorithm">The outer JWE content-encryption algorithm.</param>
    /// <param name="policy">The algorithm allowlist and safety limits for both layers.</param>
    /// <returns>A result containing a nested signed-then-encrypted JWT or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult CreateSignedThenEncrypted(
        JwtClaims claims,
        TokenKey signingKey,
        TokenKey encryptionKey,
        string contentEncryptionAlgorithm,
        TokenPolicy policy)
    {
        return JwtCreator.CreateSignedThenEncrypted(
            claims,
            signingKey,
            encryptionKey,
            contentEncryptionAlgorithm,
            policy);
    }

    /// <summary>
    /// Creates a signed compact JWT and encrypts it using caller-supplied headers for both layers.
    /// </summary>
    /// <param name="claims">The claim set to sign and encrypt.</param>
    /// <param name="signingKey">The key used for the inner JWS.</param>
    /// <param name="encryptionKey">The key used for the outer JWE.</param>
    /// <param name="signingHeader">The protected header to copy for the inner JWS.</param>
    /// <param name="encryptionHeader">The protected header to copy for the outer JWE.</param>
    /// <param name="policy">The algorithm allowlist and safety limits for both layers.</param>
    /// <returns>A result containing a nested signed-then-encrypted JWT or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult CreateSignedThenEncrypted(
        JwtClaims claims,
        TokenKey signingKey,
        TokenKey encryptionKey,
        JoseHeader signingHeader,
        JoseHeader encryptionHeader,
        TokenPolicy policy)
    {
        return JwtCreator.CreateSignedThenEncrypted(
            claims,
            signingKey,
            encryptionKey,
            signingHeader,
            encryptionHeader,
            policy);
    }

    /// <summary>
    /// Creates an encrypted compact JWT using key metadata and a content-encryption algorithm.
    /// </summary>
    /// <param name="claims">The claim set to encrypt.</param>
    /// <param name="encryptionKey">The recipient or key-encryption key.</param>
    /// <param name="contentEncryptionAlgorithm">The JWE content-encryption algorithm.</param>
    /// <param name="policy">The explicit algorithm allowlist and safety limits.</param>
    /// <returns>A result containing compact JWT text or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult CreateEncrypted(
        JwtClaims claims,
        TokenKey encryptionKey,
        string contentEncryptionAlgorithm,
        TokenPolicy policy)
    {
        return JwtCreator.CreateEncrypted(
            claims,
            encryptionKey,
            contentEncryptionAlgorithm,
            policy);
    }

    /// <summary>
    /// Creates an encrypted compact JWT using caller-supplied protected-header parameters.
    /// </summary>
    /// <param name="claims">The claim set to encrypt.</param>
    /// <param name="encryptionKey">The recipient or key-encryption key.</param>
    /// <param name="protectedHeader">The protected JWE header to copy and complete.</param>
    /// <param name="policy">The explicit algorithm allowlist and safety limits.</param>
    /// <returns>A result containing compact JWT text or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult CreateEncrypted(
        JwtClaims claims,
        TokenKey encryptionKey,
        JoseHeader protectedHeader,
        TokenPolicy policy)
    {
        return JwtCreator.CreateEncrypted(
            claims,
            encryptionKey,
            protectedHeader,
            policy);
    }

    /// <summary>
    /// Creates a signed compact JWT using protected-header defaults from the signing key.
    /// </summary>
    /// <param name="claims">The claim set to sign.</param>
    /// <param name="signingKey">The signing key and its operation metadata.</param>
    /// <param name="policy">The explicit algorithm allowlist and safety limits.</param>
    /// <returns>A result containing compact JWT text or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult CreateSigned(
        JwtClaims claims,
        TokenKey signingKey,
        TokenPolicy policy)
    {
        return JwtCreator.CreateSigned(claims, signingKey, policy);
    }

    /// <summary>
    /// Creates a signed compact JWT using caller-supplied protected-header parameters.
    /// </summary>
    /// <param name="claims">The claim set to sign.</param>
    /// <param name="signingKey">The signing key and its operation metadata.</param>
    /// <param name="protectedHeader">The protected JOSE header to copy and complete.</param>
    /// <param name="policy">The explicit algorithm allowlist and safety limits.</param>
    /// <returns>A result containing compact JWT text or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenTextResult CreateSigned(
        JwtClaims claims,
        TokenKey signingKey,
        JoseHeader protectedHeader,
        TokenPolicy policy)
    {
        return JwtCreator.CreateSigned(
            claims,
            signingKey,
            protectedHeader,
            policy);
    }
}
