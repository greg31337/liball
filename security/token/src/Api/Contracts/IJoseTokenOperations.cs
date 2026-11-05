using System;
namespace token;

/// <summary>Reads and validates compact and JSON JOSE serializations.</summary>
public interface IJoseTokenOperations
{
    /// <summary>Reads a duplicate-safe JOSE header JSON object.</summary>
    /// <param name="json">The header JSON text.</param>
    /// <param name="maximumDepth">The maximum JSON nesting depth.</param>
    /// <returns>The parsed header, or stable failure details.</returns>
    JoseHeaderResult ReadJoseHeader(string json, int maximumDepth);

    /// <summary>Reads a duplicate-safe UTF-8 JOSE header JSON object.</summary>
    /// <param name="utf8">The UTF-8 header bytes.</param>
    /// <param name="maximumDepth">The maximum JSON nesting depth.</param>
    /// <returns>The parsed header, or stable failure details.</returns>
    JoseHeaderResult ReadJoseHeaderUtf8(byte[] utf8, int maximumDepth);

    /// <summary>Writes a JOSE header as compact JSON.</summary>
    /// <param name="header">The header to write.</param>
    /// <returns>The compact JSON, or stable failure details.</returns>
    TokenTextResult WriteJoseHeader(JoseHeader header);

    /// <summary>Writes a JOSE header as compact UTF-8 JSON.</summary>
    /// <param name="header">The header to write.</param>
    /// <returns>The compact UTF-8 JSON, or stable failure details.</returns>
    TokenBytesResult WriteJoseHeaderUtf8(JoseHeader header);

    /// <summary>Validates critical parameters and protected/unprotected header uniqueness.</summary>
    /// <param name="protectedHeader">The protected header.</param>
    /// <param name="unprotectedHeaders">The associated unprotected headers.</param>
    /// <param name="understoodParameters">The extension parameters understood by the caller.</param>
    /// <returns>A success result, or stable failure details.</returns>
    TokenResult ValidateJoseCriticalHeaders(JoseHeader protectedHeader, JoseHeader[] unprotectedHeaders, string[] understoodParameters);

    /// <summary>Creates a policy-authorized unsecured compact JWS.</summary>
    /// <param name="payload">The attached payload.</param>
    /// <param name="protectedHeader">The protected header using <c>none</c>.</param>
    /// <param name="policy">The unsecured-token and resource policy.</param>
    /// <returns>The compact JWS, or stable failure details.</returns>
    TokenTextResult CreateUnsecuredJwsCompact(byte[] payload, JoseHeader protectedHeader, TokenPolicy policy);

    /// <summary>Creates a policy-authorized unsecured compact JWS from a payload span.</summary>
    /// <param name="payload">The attached payload.</param>
    /// <param name="protectedHeader">The protected header using <c>none</c>.</param>
    /// <param name="policy">The unsecured-token and resource policy.</param>
    /// <returns>The compact JWS, or stable failure details.</returns>
    TokenTextResult CreateUnsecuredJwsCompactSpan(ReadOnlySpan<byte> payload, JoseHeader protectedHeader, TokenPolicy policy);

    /// <summary>Signs an attached compact JWS.</summary>
    /// <param name="payload">The payload to sign.</param>
    /// <param name="protectedHeader">The protected header.</param>
    /// <param name="key">The signing key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The compact JWS, or stable failure details.</returns>
    TokenTextResult SignJwsCompact(byte[] payload, JoseHeader protectedHeader, TokenKey key, TokenPolicy policy);

    /// <summary>Signs an attached compact JWS from a payload span.</summary>
    /// <param name="payload">The payload to sign.</param>
    /// <param name="protectedHeader">The protected header.</param>
    /// <param name="key">The signing key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The compact JWS, or stable failure details.</returns>
    TokenTextResult SignJwsCompactSpan(ReadOnlySpan<byte> payload, JoseHeader protectedHeader, TokenKey key, TokenPolicy policy);

    /// <summary>Signs a detached compact JWS.</summary>
    /// <param name="payload">The external payload to sign.</param>
    /// <param name="protectedHeader">The protected header.</param>
    /// <param name="key">The signing key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The compact JWS with an empty payload segment, or stable failure details.</returns>
    TokenTextResult SignDetachedJwsCompact(byte[] payload, JoseHeader protectedHeader, TokenKey key, TokenPolicy policy);

    /// <summary>Signs a detached compact JWS from a payload span.</summary>
    /// <param name="payload">The external payload to sign.</param>
    /// <param name="protectedHeader">The protected header.</param>
    /// <param name="key">The signing key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The compact JWS with an empty payload segment, or stable failure details.</returns>
    TokenTextResult SignDetachedJwsCompactSpan(ReadOnlySpan<byte> payload, JoseHeader protectedHeader, TokenKey key, TokenPolicy policy);

    /// <summary>Reads a compact JWS.</summary>
    /// <param name="text">The compact JWS text.</param>
    /// <param name="policy">The algorithm and resource controls.</param>
    /// <returns>The parsed JWS, or stable failure details.</returns>
    JwsCompactResult ReadJwsCompact(string text, TokenPolicy policy);

    /// <summary>Reads an unauthenticated compact JWS payload when diagnostics are enabled.</summary>
    /// <param name="text">The compact JWS text.</param>
    /// <param name="policy">The diagnostic and resource policy.</param>
    /// <returns>The parsed JWS, or stable failure details.</returns>
    JwsCompactResult ReadJwsCompactDiagnostic(string text, TokenPolicy policy);

    /// <summary>Verifies an attached compact JWS.</summary>
    /// <param name="text">The compact JWS text.</param>
    /// <param name="key">The verification key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated JWS, or stable failure details.</returns>
    JwsCompactResult VerifyJwsCompact(string text, TokenKey key, TokenPolicy policy);

    /// <summary>Verifies a detached compact JWS.</summary>
    /// <param name="text">The compact JWS text.</param>
    /// <param name="payload">The external payload.</param>
    /// <param name="key">The verification key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated JWS, or stable failure details.</returns>
    JwsCompactResult VerifyDetachedJwsCompact(string text, byte[] payload, TokenKey key, TokenPolicy policy);

    /// <summary>Verifies a detached compact JWS against a payload span.</summary>
    /// <param name="text">The compact JWS text.</param>
    /// <param name="payload">The external payload.</param>
    /// <param name="key">The verification key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated JWS, or stable failure details.</returns>
    JwsCompactResult VerifyDetachedJwsCompactSpan(string text, ReadOnlySpan<byte> payload, TokenKey key, TokenPolicy policy);

    /// <summary>Validates an explicitly policy-authorized unsecured compact JWS.</summary>
    /// <param name="text">The compact JWS text.</param>
    /// <param name="policy">The unsecured-token and resource policy.</param>
    /// <returns>The validated JWS, or stable failure details.</returns>
    JwsCompactResult ValidateUnsecuredJwsCompact(string text, TokenPolicy policy);

    /// <summary>Creates a policy-authorized unsecured flattened JWS.</summary>
    /// <param name="payload">The attached payload.</param>
    /// <param name="protectedHeader">The protected header using <c>none</c>.</param>
    /// <param name="unprotectedHeader">The optional unprotected header.</param>
    /// <param name="policy">The unsecured-token and resource policy.</param>
    /// <returns>The flattened JWS, or stable failure details.</returns>
    TokenTextResult CreateUnsecuredJwsFlattened(byte[] payload, JoseHeader protectedHeader, JoseHeader? unprotectedHeader, TokenPolicy policy);

    /// <summary>Writes an attached flattened JWS from decoded components.</summary>
    /// <param name="payload">The attached payload.</param>
    /// <param name="protectedHeader">The protected header.</param>
    /// <param name="unprotectedHeader">The optional unprotected header.</param>
    /// <param name="signature">The signature bytes.</param>
    /// <param name="policy">The header and resource policy.</param>
    /// <returns>The flattened JWS, or stable failure details.</returns>
    TokenTextResult WriteJwsFlattened(byte[] payload, JoseHeader protectedHeader, JoseHeader? unprotectedHeader, byte[] signature, TokenPolicy policy);

    /// <summary>Writes a detached flattened JWS from decoded components.</summary>
    /// <param name="protectedHeader">The protected header.</param>
    /// <param name="unprotectedHeader">The optional unprotected header.</param>
    /// <param name="signature">The signature bytes.</param>
    /// <param name="policy">The header and resource policy.</param>
    /// <returns>The detached flattened JWS, or stable failure details.</returns>
    TokenTextResult WriteDetachedJwsFlattened(JoseHeader protectedHeader, JoseHeader? unprotectedHeader, byte[] signature, TokenPolicy policy);

    /// <summary>Signs an attached flattened JWS.</summary>
    /// <param name="payload">The payload to sign.</param>
    /// <param name="protectedHeader">The protected header.</param>
    /// <param name="unprotectedHeader">The optional unprotected header.</param>
    /// <param name="key">The signing key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The flattened JWS, or stable failure details.</returns>
    TokenTextResult SignJwsFlattened(byte[] payload, JoseHeader protectedHeader, JoseHeader? unprotectedHeader, TokenKey key, TokenPolicy policy);

    /// <summary>Signs a detached flattened JWS.</summary>
    /// <param name="payload">The external payload to sign.</param>
    /// <param name="protectedHeader">The protected header.</param>
    /// <param name="unprotectedHeader">The optional unprotected header.</param>
    /// <param name="key">The signing key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The detached flattened JWS, or stable failure details.</returns>
    TokenTextResult SignDetachedJwsFlattened(byte[] payload, JoseHeader protectedHeader, JoseHeader? unprotectedHeader, TokenKey key, TokenPolicy policy);

    /// <summary>Reads a flattened JWS JSON serialization.</summary>
    /// <param name="text">The JWS JSON text.</param>
    /// <param name="policy">The algorithm and resource controls.</param>
    /// <returns>The parsed JWS, or stable failure details.</returns>
    JwsFlattenedResult ReadJwsFlattened(string text, TokenPolicy policy);

    /// <summary>Reads an unauthenticated flattened JWS payload when diagnostics are enabled.</summary>
    /// <param name="text">The flattened JWS JSON text.</param>
    /// <param name="policy">The diagnostic and resource policy.</param>
    /// <returns>The parsed JWS, or stable failure details.</returns>
    JwsFlattenedResult ReadJwsFlattenedDiagnostic(string text, TokenPolicy policy);

    /// <summary>Verifies an attached flattened JWS.</summary>
    /// <param name="text">The flattened JWS JSON text.</param>
    /// <param name="key">The verification key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated JWS, or stable failure details.</returns>
    JwsFlattenedResult VerifyJwsFlattened(string text, TokenKey key, TokenPolicy policy);

    /// <summary>Verifies a detached flattened JWS.</summary>
    /// <param name="text">The flattened JWS JSON text.</param>
    /// <param name="payload">The external payload.</param>
    /// <param name="key">The verification key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated JWS, or stable failure details.</returns>
    JwsFlattenedResult VerifyDetachedJwsFlattened(string text, byte[] payload, TokenKey key, TokenPolicy policy);

    /// <summary>Validates an explicitly policy-authorized unsecured flattened JWS.</summary>
    /// <param name="text">The flattened JWS JSON text.</param>
    /// <param name="policy">The unsecured-token and resource policy.</param>
    /// <returns>The validated JWS, or stable failure details.</returns>
    JwsFlattenedResult ValidateUnsecuredJwsFlattened(string text, TokenPolicy policy);

    /// <summary>Creates a policy-authorized unsecured general JWS.</summary>
    /// <param name="payload">The attached shared payload.</param>
    /// <param name="protectedHeaders">The protected headers using <c>none</c>.</param>
    /// <param name="unprotectedHeaders">The optional matching unprotected headers.</param>
    /// <param name="policy">The unsecured-token and resource policy.</param>
    /// <returns>The general JWS, or stable failure details.</returns>
    TokenTextResult CreateUnsecuredJwsGeneral(byte[] payload, JoseHeader[] protectedHeaders, JoseHeader?[]? unprotectedHeaders, TokenPolicy policy);

    /// <summary>Writes an attached general JWS from decoded components.</summary>
    /// <param name="payload">The attached shared payload.</param>
    /// <param name="protectedHeaders">The protected headers.</param>
    /// <param name="unprotectedHeaders">The optional matching unprotected headers.</param>
    /// <param name="signatures">The matching signature byte arrays.</param>
    /// <param name="policy">The header and resource policy.</param>
    /// <returns>The general JWS, or stable failure details.</returns>
    TokenTextResult WriteJwsGeneral(byte[] payload, JoseHeader[] protectedHeaders, JoseHeader?[]? unprotectedHeaders, byte[][] signatures, TokenPolicy policy);

    /// <summary>Writes a detached general JWS from decoded components.</summary>
    /// <param name="protectedHeaders">The protected headers.</param>
    /// <param name="unprotectedHeaders">The optional matching unprotected headers.</param>
    /// <param name="signatures">The matching signature byte arrays.</param>
    /// <param name="policy">The header and resource policy.</param>
    /// <returns>The detached general JWS, or stable failure details.</returns>
    TokenTextResult WriteDetachedJwsGeneral(JoseHeader[] protectedHeaders, JoseHeader?[]? unprotectedHeaders, byte[][] signatures, TokenPolicy policy);

    /// <summary>Signs an attached general JWS with multiple keys.</summary>
    /// <param name="payload">The shared payload to sign.</param>
    /// <param name="protectedHeaders">The protected headers.</param>
    /// <param name="unprotectedHeaders">The optional matching unprotected headers.</param>
    /// <param name="keys">The matching signing keys.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The general JWS, or stable failure details.</returns>
    TokenTextResult SignJwsGeneral(byte[] payload, JoseHeader[] protectedHeaders, JoseHeader?[]? unprotectedHeaders, TokenKey[] keys, TokenPolicy policy);

    /// <summary>Signs a detached general JWS with multiple keys.</summary>
    /// <param name="payload">The external shared payload.</param>
    /// <param name="protectedHeaders">The protected headers.</param>
    /// <param name="unprotectedHeaders">The optional matching unprotected headers.</param>
    /// <param name="keys">The matching signing keys.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The detached general JWS, or stable failure details.</returns>
    TokenTextResult SignDetachedJwsGeneral(byte[] payload, JoseHeader[] protectedHeaders, JoseHeader?[]? unprotectedHeaders, TokenKey[] keys, TokenPolicy policy);

    /// <summary>Reads a general JWS JSON serialization.</summary>
    /// <param name="text">The JWS JSON text.</param>
    /// <param name="policy">The algorithm and resource controls.</param>
    /// <returns>The parsed JWS, or stable failure details.</returns>
    JwsGeneralResult ReadJwsGeneral(string text, TokenPolicy policy);

    /// <summary>Reads an unauthenticated general JWS payload when diagnostics are enabled.</summary>
    /// <param name="text">The general JWS JSON text.</param>
    /// <param name="policy">The diagnostic and resource policy.</param>
    /// <returns>The parsed JWS, or stable failure details.</returns>
    JwsGeneralResult ReadJwsGeneralDiagnostic(string text, TokenPolicy policy);

    /// <summary>Verifies every signature in an attached general JWS.</summary>
    /// <param name="text">The general JWS JSON text.</param>
    /// <param name="keys">The verification keys in signature order.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated JWS, or stable failure details.</returns>
    JwsGeneralResult VerifyJwsGeneral(string text, TokenKey[] keys, TokenPolicy policy);

    /// <summary>Verifies every signature in a detached general JWS.</summary>
    /// <param name="text">The general JWS JSON text.</param>
    /// <param name="payload">The external shared payload.</param>
    /// <param name="keys">The verification keys in signature order.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated JWS, or stable failure details.</returns>
    JwsGeneralResult VerifyDetachedJwsGeneral(string text, byte[] payload, TokenKey[] keys, TokenPolicy policy);

    /// <summary>Validates an explicitly policy-authorized unsecured general JWS.</summary>
    /// <param name="text">The general JWS JSON text.</param>
    /// <param name="policy">The unsecured-token and resource policy.</param>
    /// <returns>The validated JWS, or stable failure details.</returns>
    JwsGeneralResult ValidateUnsecuredJwsGeneral(string text, TokenPolicy policy);

    /// <summary>Encrypts plaintext as a compact JWE.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="protectedHeader">The protected JWE header.</param>
    /// <param name="key">The recipient or key-encryption key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The compact JWE, or stable failure details.</returns>
    TokenTextResult EncryptJweCompact(byte[] plaintext, JoseHeader protectedHeader, TokenKey key, TokenPolicy policy);

    /// <summary>Encrypts a plaintext span as a compact JWE.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="protectedHeader">The protected JWE header.</param>
    /// <param name="key">The recipient or key-encryption key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The compact JWE, or stable failure details.</returns>
    TokenTextResult EncryptJweCompactSpan(ReadOnlySpan<byte> plaintext, JoseHeader protectedHeader, TokenKey key, TokenPolicy policy);

    /// <summary>Reads a compact JWE.</summary>
    /// <param name="text">The compact JWE text.</param>
    /// <param name="policy">The algorithm and resource controls.</param>
    /// <returns>The parsed JWE, or stable failure details.</returns>
    JweCompactResult ReadJweCompact(string text, TokenPolicy policy);

    /// <summary>Reads compact JWE ciphertext when diagnostics are enabled.</summary>
    /// <param name="text">The compact JWE text.</param>
    /// <param name="policy">The diagnostic and resource policy.</param>
    /// <returns>The parsed JWE, or stable failure details.</returns>
    JweCompactResult ReadJweCompactDiagnostic(string text, TokenPolicy policy);

    /// <summary>Authenticates and decrypts a compact JWE.</summary>
    /// <param name="text">The compact JWE text.</param>
    /// <param name="key">The recipient or key-encryption key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated JWE, or stable failure details.</returns>
    JweCompactResult DecryptJweCompact(string text, TokenKey key, TokenPolicy policy);

    /// <summary>Writes flattened JWE JSON from decoded encrypted components.</summary>
    /// <param name="ciphertext">The ciphertext bytes.</param>
    /// <param name="protectedHeader">The protected header.</param>
    /// <param name="sharedUnprotectedHeader">The optional shared unprotected header.</param>
    /// <param name="recipientHeader">The optional recipient header.</param>
    /// <param name="encryptedKey">The encrypted content key.</param>
    /// <param name="initializationVector">The content-encryption IV.</param>
    /// <param name="authenticationTag">The content-encryption tag.</param>
    /// <param name="policy">The header and resource policy.</param>
    /// <returns>The flattened JWE, or stable failure details.</returns>
    TokenTextResult WriteJweFlattened(byte[] ciphertext, JoseHeader protectedHeader, JoseHeader? sharedUnprotectedHeader, JoseHeader? recipientHeader, byte[] encryptedKey, byte[] initializationVector, byte[] authenticationTag, TokenPolicy policy);

    /// <summary>Writes flattened JWE JSON with external authenticated data.</summary>
    /// <param name="ciphertext">The ciphertext bytes.</param>
    /// <param name="additionalAuthenticatedData">The external authenticated data.</param>
    /// <param name="protectedHeader">The protected header.</param>
    /// <param name="sharedUnprotectedHeader">The optional shared unprotected header.</param>
    /// <param name="recipientHeader">The optional recipient header.</param>
    /// <param name="encryptedKey">The encrypted content key.</param>
    /// <param name="initializationVector">The content-encryption IV.</param>
    /// <param name="authenticationTag">The content-encryption tag.</param>
    /// <param name="policy">The header and resource policy.</param>
    /// <returns>The flattened JWE, or stable failure details.</returns>
    TokenTextResult WriteJweFlattenedWithAuthenticatedData(byte[] ciphertext, byte[] additionalAuthenticatedData, JoseHeader protectedHeader, JoseHeader? sharedUnprotectedHeader, JoseHeader? recipientHeader, byte[] encryptedKey, byte[] initializationVector, byte[] authenticationTag, TokenPolicy policy);

    /// <summary>Encrypts plaintext as flattened JWE JSON for one recipient.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="protectedHeader">The protected header.</param>
    /// <param name="sharedUnprotectedHeader">The optional shared unprotected header.</param>
    /// <param name="recipientHeader">The optional recipient header.</param>
    /// <param name="key">The recipient or key-encryption key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The flattened JWE, or stable failure details.</returns>
    TokenTextResult EncryptJweFlattened(byte[] plaintext, JoseHeader protectedHeader, JoseHeader? sharedUnprotectedHeader, JoseHeader? recipientHeader, TokenKey key, TokenPolicy policy);

    /// <summary>Encrypts flattened JWE JSON with external authenticated data.</summary>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <param name="additionalAuthenticatedData">The external authenticated data.</param>
    /// <param name="protectedHeader">The protected header.</param>
    /// <param name="sharedUnprotectedHeader">The optional shared unprotected header.</param>
    /// <param name="recipientHeader">The optional recipient header.</param>
    /// <param name="key">The recipient or key-encryption key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The flattened JWE, or stable failure details.</returns>
    TokenTextResult EncryptJweFlattenedWithAuthenticatedData(byte[] plaintext, byte[] additionalAuthenticatedData, JoseHeader protectedHeader, JoseHeader? sharedUnprotectedHeader, JoseHeader? recipientHeader, TokenKey key, TokenPolicy policy);

    /// <summary>Reads a flattened JWE JSON serialization.</summary>
    /// <param name="text">The JWE JSON text.</param>
    /// <param name="policy">The algorithm and resource controls.</param>
    /// <returns>The parsed JWE, or stable failure details.</returns>
    JweFlattenedResult ReadJweFlattened(string text, TokenPolicy policy);

    /// <summary>Reads flattened JWE ciphertext when diagnostics are enabled.</summary>
    /// <param name="text">The flattened JWE JSON text.</param>
    /// <param name="policy">The diagnostic and resource policy.</param>
    /// <returns>The parsed JWE, or stable failure details.</returns>
    JweFlattenedResult ReadJweFlattenedDiagnostic(string text, TokenPolicy policy);

    /// <summary>Authenticates and decrypts flattened JWE JSON.</summary>
    /// <param name="text">The flattened JWE JSON text.</param>
    /// <param name="key">The recipient or key-encryption key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated JWE, or stable failure details.</returns>
    JweFlattenedResult DecryptJweFlattened(string text, TokenKey key, TokenPolicy policy);

    /// <summary>Writes general JWE JSON from decoded encrypted components.</summary>
    /// <param name="ciphertext">The shared ciphertext.</param>
    /// <param name="protectedHeader">The protected header.</param>
    /// <param name="sharedUnprotectedHeader">The optional shared unprotected header.</param>
    /// <param name="recipientHeaders">The recipient headers.</param>
    /// <param name="encryptedKeys">The encrypted content keys.</param>
    /// <param name="initializationVector">The content-encryption IV.</param>
    /// <param name="authenticationTag">The content-encryption tag.</param>
    /// <param name="policy">The header and resource policy.</param>
    /// <returns>The general JWE, or stable failure details.</returns>
    TokenTextResult WriteJweGeneral(byte[] ciphertext, JoseHeader protectedHeader, JoseHeader? sharedUnprotectedHeader, JoseHeader?[] recipientHeaders, byte[][] encryptedKeys, byte[] initializationVector, byte[] authenticationTag, TokenPolicy policy);

    /// <summary>Writes general JWE JSON with external authenticated data.</summary>
    /// <param name="ciphertext">The shared ciphertext.</param>
    /// <param name="additionalAuthenticatedData">The external authenticated data.</param>
    /// <param name="protectedHeader">The protected header.</param>
    /// <param name="sharedUnprotectedHeader">The optional shared unprotected header.</param>
    /// <param name="recipientHeaders">The recipient headers.</param>
    /// <param name="encryptedKeys">The encrypted content keys.</param>
    /// <param name="initializationVector">The content-encryption IV.</param>
    /// <param name="authenticationTag">The content-encryption tag.</param>
    /// <param name="policy">The header and resource policy.</param>
    /// <returns>The general JWE, or stable failure details.</returns>
    TokenTextResult WriteJweGeneralWithAuthenticatedData(byte[] ciphertext, byte[] additionalAuthenticatedData, JoseHeader protectedHeader, JoseHeader? sharedUnprotectedHeader, JoseHeader?[] recipientHeaders, byte[][] encryptedKeys, byte[] initializationVector, byte[] authenticationTag, TokenPolicy policy);

    /// <summary>Encrypts plaintext once for one or more general JWE recipients.</summary>
    /// <param name="plaintext">The shared plaintext.</param>
    /// <param name="protectedHeader">The protected header.</param>
    /// <param name="sharedUnprotectedHeader">The optional shared unprotected header.</param>
    /// <param name="recipientHeaders">The recipient headers.</param>
    /// <param name="keys">The matching recipient keys.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The general JWE, or stable failure details.</returns>
    TokenTextResult EncryptJweGeneral(byte[] plaintext, JoseHeader protectedHeader, JoseHeader? sharedUnprotectedHeader, JoseHeader?[] recipientHeaders, TokenKey[] keys, TokenPolicy policy);

    /// <summary>Encrypts general JWE JSON with external authenticated data.</summary>
    /// <param name="plaintext">The shared plaintext.</param>
    /// <param name="additionalAuthenticatedData">The external authenticated data.</param>
    /// <param name="protectedHeader">The protected header.</param>
    /// <param name="sharedUnprotectedHeader">The optional shared unprotected header.</param>
    /// <param name="recipientHeaders">The recipient headers.</param>
    /// <param name="keys">The matching recipient keys.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The general JWE, or stable failure details.</returns>
    TokenTextResult EncryptJweGeneralWithAuthenticatedData(byte[] plaintext, byte[] additionalAuthenticatedData, JoseHeader protectedHeader, JoseHeader? sharedUnprotectedHeader, JoseHeader?[] recipientHeaders, TokenKey[] keys, TokenPolicy policy);

    /// <summary>Reads a general JWE JSON serialization.</summary>
    /// <param name="text">The JWE JSON text.</param>
    /// <param name="policy">The algorithm and resource controls.</param>
    /// <returns>The parsed JWE, or stable failure details.</returns>
    JweGeneralResult ReadJweGeneral(string text, TokenPolicy policy);

    /// <summary>Reads general JWE ciphertext when diagnostics are enabled.</summary>
    /// <param name="text">The general JWE JSON text.</param>
    /// <param name="policy">The diagnostic and resource policy.</param>
    /// <returns>The parsed JWE, or stable failure details.</returns>
    JweGeneralResult ReadJweGeneralDiagnostic(string text, TokenPolicy policy);

    /// <summary>Decrypts one selected general JWE recipient.</summary>
    /// <param name="text">The general JWE JSON text.</param>
    /// <param name="recipientIndex">The zero-based recipient index.</param>
    /// <param name="key">The selected recipient key.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated JWE, or stable failure details.</returns>
    JweGeneralResult DecryptJweGeneralRecipient(string text, int recipientIndex, TokenKey key, TokenPolicy policy);

    /// <summary>Decrypts and authenticates every general JWE recipient.</summary>
    /// <param name="text">The general JWE JSON text.</param>
    /// <param name="keys">The recipient keys in recipient order.</param>
    /// <param name="policy">The algorithm and resource policy.</param>
    /// <returns>The authenticated JWE, or stable failure details.</returns>
    JweGeneralResult DecryptJweGeneralAll(string text, TokenKey[] keys, TokenPolicy policy);

    /// <summary>Reads a nested token from an authenticated compact JWS payload.</summary>
    /// <param name="outer">The verified outer compact JWS.</param>
    /// <param name="policy">The algorithm and nesting controls.</param>
    /// <returns>The outer and nested token models, or stable failure details.</returns>
    NestedJoseResult ReadNestedJwsCompact(JwsCompactResult outer, TokenPolicy policy);

    /// <summary>Reads a nested token from an authenticated flattened JWS payload.</summary>
    /// <param name="outer">The verified outer flattened JWS.</param>
    /// <param name="policy">The algorithm and nesting controls.</param>
    /// <returns>The outer and nested token models, or stable failure details.</returns>
    NestedJoseResult ReadNestedJwsFlattened(JwsFlattenedResult outer, TokenPolicy policy);

    /// <summary>Reads a nested token from an authenticated general JWS payload.</summary>
    /// <param name="outer">The fully verified outer general JWS.</param>
    /// <param name="policy">The algorithm and nesting controls.</param>
    /// <returns>The outer and nested token models, or stable failure details.</returns>
    NestedJoseResult ReadNestedJwsGeneral(JwsGeneralResult outer, TokenPolicy policy);

    /// <summary>Reads a nested token from authenticated compact JWE plaintext.</summary>
    /// <param name="outer">The authenticated outer compact JWE.</param>
    /// <param name="policy">The algorithm and nesting controls.</param>
    /// <returns>The parsed nested token, or stable failure details.</returns>
    NestedJoseResult ReadNestedJweCompact(JweCompactResult outer, TokenPolicy policy);

    /// <summary>Reads a nested token from authenticated flattened JWE plaintext.</summary>
    /// <param name="outer">The authenticated outer flattened JWE.</param>
    /// <param name="policy">The algorithm and nesting controls.</param>
    /// <returns>The parsed nested token, or stable failure details.</returns>
    NestedJoseResult ReadNestedJweFlattened(JweFlattenedResult outer, TokenPolicy policy);

    /// <summary>Reads a nested token from authenticated general JWE plaintext.</summary>
    /// <param name="outer">The authenticated outer general JWE.</param>
    /// <param name="policy">The algorithm and nesting controls.</param>
    /// <returns>The parsed nested token, or stable failure details.</returns>
    NestedJoseResult ReadNestedJweGeneral(JweGeneralResult outer, TokenPolicy policy);
}
