using System;
namespace token;

/// <summary>Provides CWT claim, confirmation, protection, creation, validation, and diagnostic operations.</summary>
public interface ICwtTokenOperations
{
    /// <summary>Creates validated CWT claims from CBOR map entries.</summary>
    /// <param name="values">The claim entries to validate and copy.</param>
    /// <param name="policy">The CBOR resource limits.</param>
    /// <returns>The validated claims, or stable failure details.</returns>
    CwtClaimsResult CreateCwtClaims(CborMapEntry[] values, TokenPolicy policy);

    /// <summary>Reads a deterministic CBOR CWT Claims Set.</summary>
    /// <param name="cbor">The encoded claim map.</param>
    /// <param name="policy">The CBOR resource limits.</param>
    /// <returns>The parsed claims, or stable failure details.</returns>
    CwtClaimsResult ReadCwtClaims(byte[] cbor, TokenPolicy policy);

    /// <summary>Writes a CWT Claims Set using deterministic CBOR map ordering.</summary>
    /// <param name="claims">The claims to encode.</param>
    /// <param name="policy">The output and CBOR resource limits.</param>
    /// <returns>The encoded claim map, or stable failure details.</returns>
    TokenBytesResult WriteCwtClaims(CwtClaims claims, TokenPolicy policy);

    /// <summary>Gets an integer-labeled CWT claim.</summary>
    /// <param name="claims">The claims to search.</param>
    /// <param name="label">The registered or private integer label.</param>
    /// <returns>The matching value, or <c>null</c> when absent.</returns>
    CborItem? GetCwtIntegerClaim(CwtClaims claims, int label);

    /// <summary>Gets a text-labeled private CWT claim.</summary>
    /// <param name="claims">The claims to search.</param>
    /// <param name="label">The case-sensitive text label.</param>
    /// <returns>The matching value, or <c>null</c> when absent.</returns>
    CborItem? GetCwtTextClaim(CwtClaims claims, string label);

    /// <summary>Adds or replaces an integer-labeled CWT claim.</summary>
    /// <param name="claims">The claims to update.</param>
    /// <param name="label">The registered or private integer label.</param>
    /// <param name="value">The CBOR value to store.</param>
    void SetCwtIntegerClaim(CwtClaims claims, int label, CborItem value);

    /// <summary>Adds or replaces a text-labeled private CWT claim.</summary>
    /// <param name="claims">The claims to update.</param>
    /// <param name="label">The case-sensitive text label.</param>
    /// <param name="value">The CBOR value to store.</param>
    void SetCwtTextClaim(CwtClaims claims, string label, CborItem value);

    /// <summary>Removes an integer-labeled CWT claim.</summary>
    /// <param name="claims">The claims to update.</param>
    /// <param name="label">The integer label to remove.</param>
    /// <returns><c>true</c> when a claim was removed.</returns>
    bool RemoveCwtIntegerClaim(CwtClaims claims, int label);

    /// <summary>Removes a text-labeled private CWT claim.</summary>
    /// <param name="claims">The claims to update.</param>
    /// <param name="label">The text label to remove.</param>
    /// <returns><c>true</c> when a claim was removed.</returns>
    bool RemoveCwtTextClaim(CwtClaims claims, string label);

    /// <summary>Validates registered claim types, required values, issuer, audience, and lifetime.</summary>
    /// <param name="claims">The claims to validate.</param>
    /// <param name="policy">The registered-claim validation policy.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    TokenResult ValidateRegisteredCwtClaims(CwtClaims claims, CwtValidationPolicy policy);

    /// <summary>Validates the optional CWT confirmation claim.</summary>
    /// <param name="claims">The claims containing the confirmation value.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    TokenResult ValidateCwtConfirmation(CwtClaims claims);

    /// <summary>Gets a directly embedded proof-of-possession COSE key.</summary>
    /// <param name="claims">The claims containing the confirmation value.</param>
    /// <returns>The parsed key, or stable failure details.</returns>
    TokenKeyResult GetCwtProofOfPossessionKey(CwtClaims claims);

    /// <summary>Sets a directly embedded proof-of-possession COSE key.</summary>
    /// <param name="claims">The claims to update.</param>
    /// <param name="key">The public or symmetric proof-of-possession key.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    TokenResult SetCwtProofOfPossessionKey(CwtClaims claims, TokenKey key);

    /// <summary>Gets the binary key identifier from a CWT confirmation claim.</summary>
    /// <param name="claims">The claims containing the confirmation value.</param>
    /// <returns>An independent copy of the key identifier, or stable failure details.</returns>
    TokenBytesResult GetCwtConfirmationKeyId(CwtClaims claims);

    /// <summary>Sets the binary key identifier in a CWT confirmation claim.</summary>
    /// <param name="claims">The claims to update.</param>
    /// <param name="keyId">The application-specific key identifier.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    TokenResult SetCwtConfirmationKeyId(CwtClaims claims, byte[] keyId);

    /// <summary>Gets an encoded encrypted COSE key from a CWT confirmation claim.</summary>
    /// <param name="claims">The claims containing the confirmation value.</param>
    /// <returns>The deterministic encoded COSE message, or stable failure details.</returns>
    TokenBytesResult GetCwtEncryptedConfirmationKey(CwtClaims claims);

    /// <summary>Sets an encoded encrypted COSE key in a CWT confirmation claim.</summary>
    /// <param name="claims">The claims to update.</param>
    /// <param name="encryptedKey">A valid encoded COSE_Encrypt or COSE_Encrypt0 message.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    TokenResult SetCwtEncryptedConfirmationKey(CwtClaims claims, byte[] encryptedKey);

    /// <summary>Reads RFC 9597 CWT claims from a COSE header.</summary>
    /// <param name="header">The protected and unprotected COSE header buckets.</param>
    /// <param name="policy">The CBOR resource limits.</param>
    /// <returns>The parsed header claims and placement, or stable failure details.</returns>
    CoseCwtClaimsResult ReadCwtClaimsFromCoseHeader(CoseHeader header, TokenPolicy policy);

    /// <summary>Sets protected RFC 9597 CWT claims in a COSE header.</summary>
    /// <param name="header">The header to update.</param>
    /// <param name="claims">The claims to store.</param>
    /// <param name="policy">The CBOR resource limits.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    TokenResult SetProtectedCwtClaimsInCoseHeader(CoseHeader header, CwtClaims claims, TokenPolicy policy);

    /// <summary>Sets unprotected RFC 9597 CWT claims in a COSE header.</summary>
    /// <param name="header">The header to update.</param>
    /// <param name="claims">The claims to store.</param>
    /// <param name="policy">The CBOR resource limits.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    TokenResult SetUnprotectedCwtClaimsInCoseHeader(CoseHeader header, CwtClaims claims, TokenPolicy policy);

    /// <summary>Removes RFC 9597 CWT claims from either COSE header bucket.</summary>
    /// <param name="header">The header to update.</param>
    /// <returns><c>true</c> when claims were removed.</returns>
    bool RemoveCwtClaimsFromCoseHeader(CoseHeader header);

    /// <summary>Validates that overlapping RFC 9597 header and payload claims are identical.</summary>
    /// <param name="header">The authenticated COSE header.</param>
    /// <param name="payloadClaims">The authenticated payload claims.</param>
    /// <param name="policy">The CBOR resource limits.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    TokenResult ValidateCoseHeaderCwtClaims(CoseHeader header, CwtClaims payloadClaims, TokenPolicy policy);

    /// <summary>Maps a registered JWT claim name to a CWT integer label.</summary>
    /// <param name="claimName">The case-sensitive JWT claim name.</param>
    /// <returns>The registry mapping result.</returns>
    ClaimMappingResult MapJwtClaimNameToCwt(string claimName);

    /// <summary>Maps a registered CWT integer label to a JWT claim name.</summary>
    /// <param name="claimLabel">The CWT claim label.</param>
    /// <returns>The registry mapping result.</returns>
    ClaimMappingResult MapCwtClaimLabelToJwt(int claimLabel);

    /// <summary>Creates a COSE_Sign1 CWT protection layer.</summary>
    /// <param name="header">The message header.</param>
    /// <param name="key">The private signing key.</param>
    /// <returns>The configured layer.</returns>
    CwtProtectionLayer CreateCwtSign1Layer(CoseHeader header, TokenKey key);

    /// <summary>Creates a COSE_Sign CWT protection layer.</summary>
    /// <param name="header">The shared message header.</param>
    /// <param name="signerHeaders">The signer headers.</param>
    /// <param name="keys">The corresponding private signing keys.</param>
    /// <returns>The configured layer.</returns>
    CwtProtectionLayer CreateCwtSignLayer(CoseHeader header, CoseHeader[] signerHeaders, TokenKey[] keys);

    /// <summary>Creates a COSE_Mac0 CWT protection layer.</summary>
    /// <param name="header">The message header.</param>
    /// <param name="key">The symmetric MAC key.</param>
    /// <returns>The configured layer.</returns>
    CwtProtectionLayer CreateCwtMac0Layer(CoseHeader header, TokenKey key);

    /// <summary>Creates a COSE_Mac CWT protection layer.</summary>
    /// <param name="header">The shared message header.</param>
    /// <param name="recipientHeaders">The recipient headers.</param>
    /// <param name="keys">The corresponding recipient keys.</param>
    /// <param name="senderKeys">Optional static ECDH sender keys.</param>
    /// <returns>The configured layer.</returns>
    CwtProtectionLayer CreateCwtMacLayer(CoseHeader header, CoseHeader[] recipientHeaders, TokenKey[] keys, TokenKey[]? senderKeys);

    /// <summary>Creates a COSE_Encrypt0 CWT protection layer.</summary>
    /// <param name="header">The message header.</param>
    /// <param name="key">The symmetric content-encryption key.</param>
    /// <returns>The configured layer.</returns>
    CwtProtectionLayer CreateCwtEncrypt0Layer(CoseHeader header, TokenKey key);

    /// <summary>Creates a COSE_Encrypt CWT protection layer.</summary>
    /// <param name="header">The shared message header.</param>
    /// <param name="recipientHeaders">The recipient headers.</param>
    /// <param name="keys">The corresponding recipient keys.</param>
    /// <param name="senderKeys">Optional static ECDH sender keys.</param>
    /// <returns>The configured layer.</returns>
    CwtProtectionLayer CreateCwtEncryptLayer(CoseHeader header, CoseHeader[] recipientHeaders, TokenKey[] keys, TokenKey[]? senderKeys);

    /// <summary>Creates a CWT selected from a key's assigned COSE algorithm.</summary>
    /// <param name="claims">The CWT Claims Set.</param>
    /// <param name="key">A key with a COSE algorithm assignment.</param>
    /// <param name="policy">The algorithm and resource controls.</param>
    /// <returns>The encoded CWT, or stable failure details.</returns>
    TokenBytesResult CreateCwtWithKey(CwtClaims claims, TokenKey key, TokenPolicy policy);

    /// <summary>Creates an untagged CWT with protection layers ordered innermost to outermost.</summary>
    /// <param name="claims">The CWT Claims Set.</param>
    /// <param name="layers">The COSE protection layers.</param>
    /// <param name="policy">The algorithm and resource controls.</param>
    /// <returns>The encoded CWT, or stable failure details.</returns>
    TokenBytesResult CreateCwt(CwtClaims claims, CwtProtectionLayer[] layers, TokenPolicy policy);

    /// <summary>Creates a tag-61 CWT with protection layers ordered innermost to outermost.</summary>
    /// <param name="claims">The CWT Claims Set.</param>
    /// <param name="layers">The COSE protection layers.</param>
    /// <param name="policy">The algorithm and resource controls.</param>
    /// <returns>The tagged encoded CWT, or stable failure details.</returns>
    TokenBytesResult CreateTaggedCwt(CwtClaims claims, CwtProtectionLayer[] layers, TokenPolicy policy);

    /// <summary>Validates the COSE protection layers and registered CWT claims.</summary>
    /// <param name="token">The encoded CWT.</param>
    /// <param name="resolver">The validation key resolver.</param>
    /// <param name="policy">The CWT validation policy.</param>
    /// <returns>The authenticated claims and protection metadata, or stable failure details.</returns>
    CwtValidationResult ValidateCwt(byte[] token, ITokenKeyResolver resolver, CwtValidationPolicy policy);

    /// <summary>Reads CWT structure and optionally unauthenticated claims for diagnostics.</summary>
    /// <param name="token">The encoded CWT.</param>
    /// <param name="policy">The diagnostic opt-in and resource controls.</param>
    /// <returns>The inspected headers and claims, or stable failure details.</returns>
    TokenInspectionResult ReadCwtDiagnostic(byte[] token, TokenPolicy policy);
}
