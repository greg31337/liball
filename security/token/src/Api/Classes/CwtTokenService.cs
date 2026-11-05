using System;
namespace token;

/// <summary>Provides CWT claim, confirmation, protection, creation, validation, and diagnostic operations.</summary>
public class CwtTokenService : ICwtTokenOperations
{
    /// <summary>Initializes a stateless CWT token service.</summary>
    public CwtTokenService()
    {
    }

    /// <summary>Creates validated CWT claims from CBOR map entries.</summary>
    public CwtClaimsResult CreateCwtClaims(
        CborMapEntry[] values,
        TokenPolicy policy)
    {
        return CwtClaims.FromMap(values, policy);
    }

    /// <summary>Reads a deterministic CBOR CWT Claims Set.</summary>
    public CwtClaimsResult ReadCwtClaims(
        byte[] cbor,
        TokenPolicy policy)
    {
        return CwtClaims.Read(cbor, policy);
    }

    /// <summary>Writes a CWT Claims Set using deterministic CBOR map ordering.</summary>
    public TokenBytesResult WriteCwtClaims(
        CwtClaims claims,
        TokenPolicy policy)
    {
        return CwtClaims.Write(claims, policy);
    }

    /// <summary>Gets an integer-labeled CWT claim.</summary>
    public CborItem? GetCwtIntegerClaim(CwtClaims claims, int label)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        return claims.Get(label);
    }

    /// <summary>Gets a text-labeled private CWT claim.</summary>
    public CborItem? GetCwtTextClaim(CwtClaims claims, string label)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        return claims.GetByTextLabel(label);
    }

    /// <summary>Adds or replaces an integer-labeled CWT claim.</summary>
    public void SetCwtIntegerClaim(
        CwtClaims claims,
        int label,
        CborItem value)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        claims.Set(label, value);
    }

    /// <summary>Adds or replaces a text-labeled private CWT claim.</summary>
    public void SetCwtTextClaim(
        CwtClaims claims,
        string label,
        CborItem value)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        claims.SetByTextLabel(label, value);
    }

    /// <summary>Removes an integer-labeled CWT claim.</summary>
    public bool RemoveCwtIntegerClaim(CwtClaims claims, int label)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        return claims.Remove(label);
    }

    /// <summary>Removes a text-labeled private CWT claim.</summary>
    public bool RemoveCwtTextClaim(CwtClaims claims, string label)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        return claims.RemoveByTextLabel(label);
    }

    /// <summary>Validates registered claim types, required values, issuer, audience, and lifetime.</summary>
    public TokenResult ValidateRegisteredCwtClaims(
        CwtClaims claims,
        CwtValidationPolicy policy)
    {
        return CwtRegisteredClaimValidator.Validate(claims, policy);
    }

    /// <summary>Validates the optional CWT confirmation claim.</summary>
    public TokenResult ValidateCwtConfirmation(CwtClaims claims)
    {
        return CwtConfirmation.Validate(claims);
    }

    /// <summary>Gets a directly embedded proof-of-possession COSE key.</summary>
    public TokenKeyResult GetCwtProofOfPossessionKey(CwtClaims claims)
    {
        return CwtConfirmation.GetProofOfPossessionKey(claims);
    }

    /// <summary>Sets a directly embedded proof-of-possession COSE key.</summary>
    public TokenResult SetCwtProofOfPossessionKey(
        CwtClaims claims,
        TokenKey key)
    {
        return CwtConfirmation.SetProofOfPossessionKey(claims, key);
    }

    /// <summary>Gets the binary key identifier from a CWT confirmation claim.</summary>
    public TokenBytesResult GetCwtConfirmationKeyId(CwtClaims claims)
    {
        return CwtConfirmation.GetKeyId(claims);
    }

    /// <summary>Sets the binary key identifier in a CWT confirmation claim.</summary>
    public TokenResult SetCwtConfirmationKeyId(
        CwtClaims claims,
        byte[] keyId)
    {
        return CwtConfirmation.SetKeyId(claims, keyId);
    }

    /// <summary>Gets an encoded encrypted COSE key from a CWT confirmation claim.</summary>
    public TokenBytesResult GetCwtEncryptedConfirmationKey(
        CwtClaims claims)
    {
        return CwtConfirmation.GetEncryptedKey(claims);
    }

    /// <summary>Sets an encoded encrypted COSE key in a CWT confirmation claim.</summary>
    public TokenResult SetCwtEncryptedConfirmationKey(
        CwtClaims claims,
        byte[] encryptedKey)
    {
        return CwtConfirmation.SetEncryptedKey(claims, encryptedKey);
    }

    /// <summary>Reads RFC 9597 CWT claims from a COSE header.</summary>
    public CoseCwtClaimsResult ReadCwtClaimsFromCoseHeader(
        CoseHeader header,
        TokenPolicy policy)
    {
        return CoseCwtClaims.Read(header, policy);
    }

    /// <summary>Sets protected RFC 9597 CWT claims in a COSE header.</summary>
    public TokenResult SetProtectedCwtClaimsInCoseHeader(
        CoseHeader header,
        CwtClaims claims,
        TokenPolicy policy)
    {
        return CoseCwtClaims.SetProtected(header, claims, policy);
    }

    /// <summary>Sets unprotected RFC 9597 CWT claims in a COSE header.</summary>
    public TokenResult SetUnprotectedCwtClaimsInCoseHeader(
        CoseHeader header,
        CwtClaims claims,
        TokenPolicy policy)
    {
        return CoseCwtClaims.SetUnprotected(header, claims, policy);
    }

    /// <summary>Removes RFC 9597 CWT claims from either COSE header bucket.</summary>
    public bool RemoveCwtClaimsFromCoseHeader(CoseHeader header)
    {
        return CoseCwtClaims.Remove(header);
    }

    /// <summary>Validates that overlapping RFC 9597 header and payload claims are identical.</summary>
    public TokenResult ValidateCoseHeaderCwtClaims(
        CoseHeader header,
        CwtClaims payloadClaims,
        TokenPolicy policy)
    {
        return CoseCwtClaims.ValidateAgainstPayload(
            header,
            payloadClaims,
            policy);
    }

    /// <summary>Maps a registered JWT claim name to a CWT integer label.</summary>
    public ClaimMappingResult MapJwtClaimNameToCwt(string claimName)
    {
        if (claimName == null)
        {
            throw new ArgumentNullException("claimName");
        }

        return JwtCwtClaimMap.FromJwt(claimName);
    }

    /// <summary>Maps a registered CWT integer label to a JWT claim name.</summary>
    public ClaimMappingResult MapCwtClaimLabelToJwt(int claimLabel)
    {
        return JwtCwtClaimMap.FromCwt(claimLabel);
    }

    /// <summary>Creates a COSE_Sign1 CWT protection layer.</summary>
    public CwtProtectionLayer CreateCwtSign1Layer(
        CoseHeader header,
        TokenKey key)
    {
        return CwtProtectionLayer.CreateSign1(header, key);
    }

    /// <summary>Creates a COSE_Sign CWT protection layer.</summary>
    public CwtProtectionLayer CreateCwtSignLayer(
        CoseHeader header,
        CoseHeader[] signerHeaders,
        TokenKey[] keys)
    {
        return CwtProtectionLayer.CreateSign(
            header,
            signerHeaders,
            keys);
    }

    /// <summary>Creates a COSE_Mac0 CWT protection layer.</summary>
    public CwtProtectionLayer CreateCwtMac0Layer(
        CoseHeader header,
        TokenKey key)
    {
        return CwtProtectionLayer.CreateMac0(header, key);
    }

    /// <summary>Creates a COSE_Mac CWT protection layer.</summary>
    public CwtProtectionLayer CreateCwtMacLayer(
        CoseHeader header,
        CoseHeader[] recipientHeaders,
        TokenKey[] keys,
        TokenKey[]? senderKeys)
    {
        return CwtProtectionLayer.CreateMac(
            header,
            recipientHeaders,
            keys,
            senderKeys);
    }

    /// <summary>Creates a COSE_Encrypt0 CWT protection layer.</summary>
    public CwtProtectionLayer CreateCwtEncrypt0Layer(
        CoseHeader header,
        TokenKey key)
    {
        return CwtProtectionLayer.CreateEncrypt0(header, key);
    }

    /// <summary>Creates a COSE_Encrypt CWT protection layer.</summary>
    public CwtProtectionLayer CreateCwtEncryptLayer(
        CoseHeader header,
        CoseHeader[] recipientHeaders,
        TokenKey[] keys,
        TokenKey[]? senderKeys)
    {
        return CwtProtectionLayer.CreateEncrypt(
            header,
            recipientHeaders,
            keys,
            senderKeys);
    }

    /// <summary>Creates a CWT selected from a key's assigned COSE algorithm.</summary>
    public TokenBytesResult CreateCwtWithKey(
        CwtClaims claims,
        TokenKey key,
        TokenPolicy policy)
    {
        return CborWebToken.Create(claims, key, policy);
    }

    /// <summary>Creates an untagged CWT with protection layers ordered innermost to outermost.</summary>
    public TokenBytesResult CreateCwt(
        CwtClaims claims,
        CwtProtectionLayer[] layers,
        TokenPolicy policy)
    {
        return CborWebToken.CreateNested(
            claims,
            layers,
            false,
            policy);
    }

    /// <summary>Creates a tag-61 CWT with protection layers ordered innermost to outermost.</summary>
    public TokenBytesResult CreateTaggedCwt(
        CwtClaims claims,
        CwtProtectionLayer[] layers,
        TokenPolicy policy)
    {
        return CborWebToken.CreateNested(
            claims,
            layers,
            true,
            policy);
    }

    /// <summary>Validates the COSE protection layers and registered CWT claims.</summary>
    public CwtValidationResult ValidateCwt(
        byte[] token,
        ITokenKeyResolver resolver,
        CwtValidationPolicy policy)
    {
        return CborWebToken.Validate(token, resolver, policy);
    }

    /// <summary>Reads CWT structure and optionally unauthenticated claims for diagnostics.</summary>
    public TokenInspectionResult ReadCwtDiagnostic(
        byte[] token,
        TokenPolicy policy)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        if (!policy.AllowUnauthenticatedDecode)
        {
            TokenInspectionResult denied = new TokenInspectionResult();
            denied.Error = new TokenError(
                "unauthenticated_decode_not_allowed",
                "Diagnostic CWT claims decoding requires explicit policy opt-in.",
                -1);
            return denied;
        }

        return UniversalToken.InspectBytes(token, policy);
    }
}
