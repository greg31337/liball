using System;

namespace token;

/// <summary>Implements JOSE header, JWS, diagnostic, and nested-token operations.</summary>
public class JoseTokenService : IJoseTokenOperations
{
    /// <summary>Initializes a stateless JOSE token service.</summary>
    public JoseTokenService()
    {
    }

    /// <summary>Reads a duplicate-safe JOSE header JSON object.</summary>
    public JoseHeaderResult ReadJoseHeader(string json, int maximumDepth)
    {
        return JoseHeader.Read(json, maximumDepth);
    }

    /// <summary>Reads a duplicate-safe UTF-8 JOSE header JSON object.</summary>
    public JoseHeaderResult ReadJoseHeaderUtf8(byte[] utf8, int maximumDepth)
    {
        return JoseHeader.Read(utf8, maximumDepth);
    }

    /// <summary>Writes a JOSE header as compact JSON.</summary>
    public TokenTextResult WriteJoseHeader(JoseHeader header)
    {
        return JoseHeader.Write(header);
    }

    /// <summary>Writes a JOSE header as compact UTF-8 JSON.</summary>
    public TokenBytesResult WriteJoseHeaderUtf8(JoseHeader header)
    {
        return JoseHeader.WriteUtf8(header);
    }

    /// <summary>Validates critical parameters and protected/unprotected header uniqueness.</summary>
    public TokenResult ValidateJoseCriticalHeaders(JoseHeader protectedHeader, JoseHeader[] unprotectedHeaders, string[] understoodParameters)
    {
        return JoseCriticalHeaderValidator.Validate(protectedHeader, unprotectedHeaders, understoodParameters);
    }

    /// <summary>Creates a policy-authorized unsecured compact JWS.</summary>
    public TokenTextResult CreateUnsecuredJwsCompact(byte[] payload, JoseHeader protectedHeader, TokenPolicy policy)
    {
        return JsonWebSignature.CreateUnsecured(payload, protectedHeader, policy);
    }

    /// <summary>Creates a policy-authorized unsecured compact JWS from a payload span.</summary>
    public TokenTextResult CreateUnsecuredJwsCompactSpan(ReadOnlySpan<byte> payload, JoseHeader protectedHeader, TokenPolicy policy)
    {
        return JsonWebSignature.CreateUnsecured(payload, protectedHeader, policy);
    }

    /// <summary>Signs an attached compact JWS.</summary>
    public TokenTextResult SignJwsCompact(byte[] payload, JoseHeader protectedHeader, TokenKey key, TokenPolicy policy)
    {
        return JsonWebSignature.Sign(payload, protectedHeader, key, policy);
    }

    /// <summary>Signs an attached compact JWS from a payload span.</summary>
    public TokenTextResult SignJwsCompactSpan(ReadOnlySpan<byte> payload, JoseHeader protectedHeader, TokenKey key, TokenPolicy policy)
    {
        return JsonWebSignature.Sign(payload, protectedHeader, key, policy);
    }

    /// <summary>Signs a detached compact JWS.</summary>
    public TokenTextResult SignDetachedJwsCompact(byte[] payload, JoseHeader protectedHeader, TokenKey key, TokenPolicy policy)
    {
        return JsonWebSignature.SignDetached(payload, protectedHeader, key, policy);
    }

    /// <summary>Signs a detached compact JWS from a payload span.</summary>
    public TokenTextResult SignDetachedJwsCompactSpan(ReadOnlySpan<byte> payload, JoseHeader protectedHeader, TokenKey key, TokenPolicy policy)
    {
        return JsonWebSignature.SignDetached(payload, protectedHeader, key, policy);
    }

    /// <summary>Reads a compact JWS.</summary>
    public JwsCompactResult ReadJwsCompact(string text, TokenPolicy policy)
    {
        return JsonWebSignature.Parse(text, policy);
    }

    /// <summary>Reads an unauthenticated compact JWS payload when diagnostics are enabled.</summary>
    public JwsCompactResult ReadJwsCompactDiagnostic(string text, TokenPolicy policy)
    {
        return JsonWebSignature.ReadDiagnostic(text, policy);
    }

    /// <summary>Verifies an attached compact JWS.</summary>
    public JwsCompactResult VerifyJwsCompact(string text, TokenKey key, TokenPolicy policy)
    {
        return JsonWebSignature.Verify(text, key, policy);
    }

    /// <summary>Verifies a detached compact JWS.</summary>
    public JwsCompactResult VerifyDetachedJwsCompact(string text, byte[] payload, TokenKey key, TokenPolicy policy)
    {
        return JsonWebSignature.VerifyDetached(text, payload, key, policy);
    }

    /// <summary>Verifies a detached compact JWS against a payload span.</summary>
    public JwsCompactResult VerifyDetachedJwsCompactSpan(string text, ReadOnlySpan<byte> payload, TokenKey key, TokenPolicy policy)
    {
        return JsonWebSignature.VerifyDetached(text, payload, key, policy);
    }

    /// <summary>Validates an explicitly policy-authorized unsecured compact JWS.</summary>
    public JwsCompactResult ValidateUnsecuredJwsCompact(string text, TokenPolicy policy)
    {
        return JsonWebSignature.ValidateUnsecured(text, policy);
    }

    /// <summary>Creates a policy-authorized unsecured flattened JWS.</summary>
    public TokenTextResult CreateUnsecuredJwsFlattened(byte[] payload, JoseHeader protectedHeader, JoseHeader? unprotectedHeader, TokenPolicy policy)
    {
        return JsonWebSignatureFlattened.CreateUnsecured(payload, protectedHeader, unprotectedHeader, policy);
    }

    /// <summary>Writes an attached flattened JWS from decoded components.</summary>
    public TokenTextResult WriteJwsFlattened(byte[] payload, JoseHeader protectedHeader, JoseHeader? unprotectedHeader, byte[] signature, TokenPolicy policy)
    {
        return JsonWebSignatureFlattened.Write(payload, protectedHeader, unprotectedHeader, signature, policy);
    }

    /// <summary>Writes a detached flattened JWS from decoded components.</summary>
    public TokenTextResult WriteDetachedJwsFlattened(JoseHeader protectedHeader, JoseHeader? unprotectedHeader, byte[] signature, TokenPolicy policy)
    {
        return JsonWebSignatureFlattened.WriteDetached(protectedHeader, unprotectedHeader, signature, policy);
    }

    /// <summary>Signs an attached flattened JWS.</summary>
    public TokenTextResult SignJwsFlattened(byte[] payload, JoseHeader protectedHeader, JoseHeader? unprotectedHeader, TokenKey key, TokenPolicy policy)
    {
        return JsonWebSignatureFlattened.Sign(payload, protectedHeader, unprotectedHeader, key, policy);
    }

    /// <summary>Signs a detached flattened JWS.</summary>
    public TokenTextResult SignDetachedJwsFlattened(byte[] payload, JoseHeader protectedHeader, JoseHeader? unprotectedHeader, TokenKey key, TokenPolicy policy)
    {
        return JsonWebSignatureFlattened.SignDetached(payload, protectedHeader, unprotectedHeader, key, policy);
    }

    /// <summary>Reads a flattened JWS JSON serialization.</summary>
    public JwsFlattenedResult ReadJwsFlattened(string text, TokenPolicy policy)
    {
        return JsonWebSignatureFlattened.Parse(text, policy);
    }

    /// <summary>Reads an unauthenticated flattened JWS payload when diagnostics are enabled.</summary>
    public JwsFlattenedResult ReadJwsFlattenedDiagnostic(string text, TokenPolicy policy)
    {
        return JsonWebSignatureFlattened.ReadDiagnostic(text, policy);
    }

    /// <summary>Verifies an attached flattened JWS.</summary>
    public JwsFlattenedResult VerifyJwsFlattened(string text, TokenKey key, TokenPolicy policy)
    {
        return JsonWebSignatureFlattened.Verify(text, key, policy);
    }

    /// <summary>Verifies a detached flattened JWS.</summary>
    public JwsFlattenedResult VerifyDetachedJwsFlattened(string text, byte[] payload, TokenKey key, TokenPolicy policy)
    {
        return JsonWebSignatureFlattened.VerifyDetached(text, payload, key, policy);
    }

    /// <summary>Validates an explicitly policy-authorized unsecured flattened JWS.</summary>
    public JwsFlattenedResult ValidateUnsecuredJwsFlattened(string text, TokenPolicy policy)
    {
        return JsonWebSignatureFlattened.ValidateUnsecured(text, policy);
    }

    /// <summary>Creates a policy-authorized unsecured general JWS.</summary>
    public TokenTextResult CreateUnsecuredJwsGeneral(byte[] payload, JoseHeader[] protectedHeaders, JoseHeader?[]? unprotectedHeaders, TokenPolicy policy)
    {
        return JsonWebSignatureGeneral.CreateUnsecured(payload, protectedHeaders, unprotectedHeaders, policy);
    }

    /// <summary>Writes an attached general JWS from decoded components.</summary>
    public TokenTextResult WriteJwsGeneral(byte[] payload, JoseHeader[] protectedHeaders, JoseHeader?[]? unprotectedHeaders, byte[][] signatures, TokenPolicy policy)
    {
        return JsonWebSignatureGeneral.Write(payload, protectedHeaders, unprotectedHeaders, signatures, policy);
    }

    /// <summary>Writes a detached general JWS from decoded components.</summary>
    public TokenTextResult WriteDetachedJwsGeneral(JoseHeader[] protectedHeaders, JoseHeader?[]? unprotectedHeaders, byte[][] signatures, TokenPolicy policy)
    {
        return JsonWebSignatureGeneral.WriteDetached(protectedHeaders, unprotectedHeaders, signatures, policy);
    }

    /// <summary>Signs an attached general JWS with multiple keys.</summary>
    public TokenTextResult SignJwsGeneral(byte[] payload, JoseHeader[] protectedHeaders, JoseHeader?[]? unprotectedHeaders, TokenKey[] keys, TokenPolicy policy)
    {
        return JsonWebSignatureGeneral.Sign(payload, protectedHeaders, unprotectedHeaders, keys, policy);
    }

    /// <summary>Signs a detached general JWS with multiple keys.</summary>
    public TokenTextResult SignDetachedJwsGeneral(byte[] payload, JoseHeader[] protectedHeaders, JoseHeader?[]? unprotectedHeaders, TokenKey[] keys, TokenPolicy policy)
    {
        return JsonWebSignatureGeneral.SignDetached(payload, protectedHeaders, unprotectedHeaders, keys, policy);
    }

    /// <summary>Reads a general JWS JSON serialization.</summary>
    public JwsGeneralResult ReadJwsGeneral(string text, TokenPolicy policy)
    {
        return JsonWebSignatureGeneral.Parse(text, policy);
    }

    /// <summary>Reads an unauthenticated general JWS payload when diagnostics are enabled.</summary>
    public JwsGeneralResult ReadJwsGeneralDiagnostic(string text, TokenPolicy policy)
    {
        return JsonWebSignatureGeneral.ReadDiagnostic(text, policy);
    }

    /// <summary>Verifies every signature in an attached general JWS.</summary>
    public JwsGeneralResult VerifyJwsGeneral(string text, TokenKey[] keys, TokenPolicy policy)
    {
        return JsonWebSignatureGeneral.Verify(text, keys, policy);
    }

    /// <summary>Verifies every signature in a detached general JWS.</summary>
    public JwsGeneralResult VerifyDetachedJwsGeneral(string text, byte[] payload, TokenKey[] keys, TokenPolicy policy)
    {
        return JsonWebSignatureGeneral.VerifyDetached(text, payload, keys, policy);
    }

    /// <summary>Validates an explicitly policy-authorized unsecured general JWS.</summary>
    public JwsGeneralResult ValidateUnsecuredJwsGeneral(string text, TokenPolicy policy)
    {
        return JsonWebSignatureGeneral.ValidateUnsecured(text, policy);
    }

    /// <summary>Encrypts plaintext as a compact JWE.</summary>
    public TokenTextResult EncryptJweCompact(byte[] plaintext, JoseHeader protectedHeader, TokenKey key, TokenPolicy policy)
    {
        return JsonWebEncryption.Encrypt(plaintext, protectedHeader, key, policy);
    }

    /// <summary>Encrypts a plaintext span as a compact JWE.</summary>
    public TokenTextResult EncryptJweCompactSpan(ReadOnlySpan<byte> plaintext, JoseHeader protectedHeader, TokenKey key, TokenPolicy policy)
    {
        return JsonWebEncryption.Encrypt(plaintext, protectedHeader, key, policy);
    }

    /// <summary>Reads a compact JWE.</summary>
    public JweCompactResult ReadJweCompact(string text, TokenPolicy policy)
    {
        return JsonWebEncryption.Parse(text, policy);
    }

    /// <summary>Reads compact JWE ciphertext when diagnostics are enabled.</summary>
    public JweCompactResult ReadJweCompactDiagnostic(string text, TokenPolicy policy)
    {
        return JsonWebEncryption.ReadDiagnostic(text, policy);
    }

    /// <summary>Authenticates and decrypts a compact JWE.</summary>
    public JweCompactResult DecryptJweCompact(string text, TokenKey key, TokenPolicy policy)
    {
        return JsonWebEncryption.Decrypt(text, key, policy);
    }

    /// <summary>Writes flattened JWE JSON from decoded encrypted components.</summary>
    public TokenTextResult WriteJweFlattened(byte[] ciphertext, JoseHeader protectedHeader, JoseHeader? sharedUnprotectedHeader, JoseHeader? recipientHeader, byte[] encryptedKey, byte[] initializationVector, byte[] authenticationTag, TokenPolicy policy)
    {
        return JsonWebEncryptionFlattened.Write(ciphertext, protectedHeader, sharedUnprotectedHeader, recipientHeader, encryptedKey, initializationVector, authenticationTag, policy);
    }

    /// <summary>Writes flattened JWE JSON with external authenticated data.</summary>
    public TokenTextResult WriteJweFlattenedWithAuthenticatedData(byte[] ciphertext, byte[] additionalAuthenticatedData, JoseHeader protectedHeader, JoseHeader? sharedUnprotectedHeader, JoseHeader? recipientHeader, byte[] encryptedKey, byte[] initializationVector, byte[] authenticationTag, TokenPolicy policy)
    {
        return JsonWebEncryptionFlattened.Write(ciphertext, additionalAuthenticatedData, protectedHeader, sharedUnprotectedHeader, recipientHeader, encryptedKey, initializationVector, authenticationTag, policy);
    }

    /// <summary>Encrypts plaintext as flattened JWE JSON for one recipient.</summary>
    public TokenTextResult EncryptJweFlattened(byte[] plaintext, JoseHeader protectedHeader, JoseHeader? sharedUnprotectedHeader, JoseHeader? recipientHeader, TokenKey key, TokenPolicy policy)
    {
        return JsonWebEncryptionFlattened.Encrypt(plaintext, protectedHeader, sharedUnprotectedHeader, recipientHeader, key, policy);
    }

    /// <summary>Encrypts flattened JWE JSON with external authenticated data.</summary>
    public TokenTextResult EncryptJweFlattenedWithAuthenticatedData(byte[] plaintext, byte[] additionalAuthenticatedData, JoseHeader protectedHeader, JoseHeader? sharedUnprotectedHeader, JoseHeader? recipientHeader, TokenKey key, TokenPolicy policy)
    {
        return JsonWebEncryptionFlattened.Encrypt(plaintext, additionalAuthenticatedData, protectedHeader, sharedUnprotectedHeader, recipientHeader, key, policy);
    }

    /// <summary>Reads a flattened JWE JSON serialization.</summary>
    public JweFlattenedResult ReadJweFlattened(string text, TokenPolicy policy)
    {
        return JsonWebEncryptionFlattened.Parse(text, policy);
    }

    /// <summary>Reads flattened JWE ciphertext when diagnostics are enabled.</summary>
    public JweFlattenedResult ReadJweFlattenedDiagnostic(string text, TokenPolicy policy)
    {
        return JsonWebEncryptionFlattened.ReadDiagnostic(text, policy);
    }

    /// <summary>Authenticates and decrypts flattened JWE JSON.</summary>
    public JweFlattenedResult DecryptJweFlattened(string text, TokenKey key, TokenPolicy policy)
    {
        return JsonWebEncryptionFlattened.Decrypt(text, key, policy);
    }

    /// <summary>Writes general JWE JSON from decoded encrypted components.</summary>
    public TokenTextResult WriteJweGeneral(byte[] ciphertext, JoseHeader protectedHeader, JoseHeader? sharedUnprotectedHeader, JoseHeader?[] recipientHeaders, byte[][] encryptedKeys, byte[] initializationVector, byte[] authenticationTag, TokenPolicy policy)
    {
        return JsonWebEncryptionGeneral.Write(ciphertext, protectedHeader, sharedUnprotectedHeader, recipientHeaders, encryptedKeys, initializationVector, authenticationTag, policy);
    }

    /// <summary>Writes general JWE JSON with external authenticated data.</summary>
    public TokenTextResult WriteJweGeneralWithAuthenticatedData(byte[] ciphertext, byte[] additionalAuthenticatedData, JoseHeader protectedHeader, JoseHeader? sharedUnprotectedHeader, JoseHeader?[] recipientHeaders, byte[][] encryptedKeys, byte[] initializationVector, byte[] authenticationTag, TokenPolicy policy)
    {
        return JsonWebEncryptionGeneral.Write(ciphertext, additionalAuthenticatedData, protectedHeader, sharedUnprotectedHeader, recipientHeaders, encryptedKeys, initializationVector, authenticationTag, policy);
    }

    /// <summary>Encrypts plaintext once for one or more general JWE recipients.</summary>
    public TokenTextResult EncryptJweGeneral(byte[] plaintext, JoseHeader protectedHeader, JoseHeader? sharedUnprotectedHeader, JoseHeader?[] recipientHeaders, TokenKey[] keys, TokenPolicy policy)
    {
        return JsonWebEncryptionGeneral.Encrypt(plaintext, protectedHeader, sharedUnprotectedHeader, recipientHeaders, keys, policy);
    }

    /// <summary>Encrypts general JWE JSON with external authenticated data.</summary>
    public TokenTextResult EncryptJweGeneralWithAuthenticatedData(byte[] plaintext, byte[] additionalAuthenticatedData, JoseHeader protectedHeader, JoseHeader? sharedUnprotectedHeader, JoseHeader?[] recipientHeaders, TokenKey[] keys, TokenPolicy policy)
    {
        return JsonWebEncryptionGeneral.Encrypt(plaintext, additionalAuthenticatedData, protectedHeader, sharedUnprotectedHeader, recipientHeaders, keys, policy);
    }

    /// <summary>Reads a general JWE JSON serialization.</summary>
    public JweGeneralResult ReadJweGeneral(string text, TokenPolicy policy)
    {
        return JsonWebEncryptionGeneral.Parse(text, policy);
    }

    /// <summary>Reads general JWE ciphertext when diagnostics are enabled.</summary>
    public JweGeneralResult ReadJweGeneralDiagnostic(string text, TokenPolicy policy)
    {
        return JsonWebEncryptionGeneral.ReadDiagnostic(text, policy);
    }

    /// <summary>Decrypts one selected general JWE recipient.</summary>
    public JweGeneralResult DecryptJweGeneralRecipient(string text, int recipientIndex, TokenKey key, TokenPolicy policy)
    {
        return JsonWebEncryptionGeneral.Decrypt(text, recipientIndex, key, policy);
    }

    /// <summary>Decrypts and authenticates every general JWE recipient.</summary>
    public JweGeneralResult DecryptJweGeneralAll(string text, TokenKey[] keys, TokenPolicy policy)
    {
        return JsonWebEncryptionGeneral.Decrypt(text, keys, policy);
    }

    /// <summary>Reads a nested token from an authenticated compact JWS payload.</summary>
    public NestedJoseResult ReadNestedJwsCompact(JwsCompactResult outer, TokenPolicy policy)
    {
        return NestedJose.Read(outer, policy);
    }

    /// <summary>Reads a nested token from an authenticated flattened JWS payload.</summary>
    public NestedJoseResult ReadNestedJwsFlattened(JwsFlattenedResult outer, TokenPolicy policy)
    {
        return NestedJose.Read(outer, policy);
    }

    /// <summary>Reads a nested token from an authenticated general JWS payload.</summary>
    public NestedJoseResult ReadNestedJwsGeneral(JwsGeneralResult outer, TokenPolicy policy)
    {
        return NestedJose.Read(outer, policy);
    }

    /// <summary>Reads a nested token from authenticated compact JWE plaintext.</summary>
    public NestedJoseResult ReadNestedJweCompact(JweCompactResult outer, TokenPolicy policy)
    {
        return NestedJose.Read(outer, policy);
    }

    /// <summary>Reads a nested token from authenticated flattened JWE plaintext.</summary>
    public NestedJoseResult ReadNestedJweFlattened(JweFlattenedResult outer, TokenPolicy policy)
    {
        return NestedJose.Read(outer, policy);
    }

    /// <summary>Reads a nested token from authenticated general JWE plaintext.</summary>
    public NestedJoseResult ReadNestedJweGeneral(JweGeneralResult outer, TokenPolicy policy)
    {
        return NestedJose.Read(outer, policy);
    }
}
