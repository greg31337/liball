using System;
namespace token;

/// <summary>Reads, writes, and validates RFC 9597 CWT claims in COSE headers.</summary>
internal static class CoseCwtClaims
{
    /// <summary>Reads the optional CWT Claims parameter with default limits.</summary>
    /// <param name="header">The COSE header.</param>
    /// <returns>The parsed claims and their protection placement.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>header</c> is null.</exception>
    internal static CoseCwtClaimsResult Read(CoseHeader header)
    {
        return Read(header, new TokenPolicy());
    }

    /// <summary>Reads the optional CWT Claims parameter with explicit limits.</summary>
    /// <param name="header">The COSE header.</param>
    /// <param name="policy">The CBOR size and depth limits.</param>
    /// <returns>The parsed claims and their protection placement.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseCwtClaimsResult Read(
        CoseHeader header,
        TokenPolicy policy)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        CborItem? protectedValue = header.GetProtected(
            CoseHeaderLabels.CwtClaims);
        CborItem? unprotectedValue = header.GetUnprotected(
            CoseHeaderLabels.CwtClaims);
        if (protectedValue != null && unprotectedValue != null)
        {
            return Failure(
                "duplicate_cose_header_label",
                "The CWT Claims parameter occurs in both COSE header buckets.");
        }

        CborItem? value = protectedValue == null
            ? unprotectedValue
            : protectedValue;
        if (value == null)
        {
            CoseCwtClaimsResult absent = new CoseCwtClaimsResult();
            absent.Success = true;
            return absent;
        }

        if (value.Kind != TokenCborValueKind.Map || value.MapValue == null)
        {
            return Failure(
                "invalid_cwt_claims_header",
                "The COSE CWT Claims header parameter must be a CBOR map.");
        }

        CwtClaimsResult parsed = CwtClaims.FromMap(value.MapValue, policy);
        if (!parsed.Success)
        {
            return Failure(parsed.Error);
        }

        CoseCwtClaimsResult result = new CoseCwtClaimsResult();
        result.Success = true;
        result.Present = true;
        result.Protected = protectedValue != null;
        result.Claims = parsed.Claims;
        return result;
    }

    /// <summary>Adds or replaces protected CWT claims in a COSE header.</summary>
    /// <param name="header">The COSE header to update.</param>
    /// <param name="claims">The CWT claims to store.</param>
    /// <param name="policy">The CBOR size and depth limits.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult SetProtected(
        CoseHeader header,
        CwtClaims claims,
        TokenPolicy policy)
    {
        return Set(header, claims, policy, true);
    }

    /// <summary>Adds or replaces unprotected CWT claims in a COSE header.</summary>
    /// <remarks>Protected placement is recommended when claims require integrity protection.</remarks>
    /// <param name="header">The COSE header to update.</param>
    /// <param name="claims">The CWT claims to store.</param>
    /// <param name="policy">The CBOR size and depth limits.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult SetUnprotected(
        CoseHeader header,
        CwtClaims claims,
        TokenPolicy policy)
    {
        return Set(header, claims, policy, false);
    }

    /// <summary>Removes the CWT Claims parameter from either COSE header bucket.</summary>
    /// <param name="header">The COSE header to update.</param>
    /// <returns><c>true</c> when a parameter was removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>header</c> is null.</exception>
    internal static bool Remove(CoseHeader header)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        return header.Remove(CoseHeaderLabels.CwtClaims);
    }

    /// <summary>Verifies identical values for claims occurring in both a COSE header and CWT payload.</summary>
    /// <param name="header">The authenticated COSE header.</param>
    /// <param name="payloadClaims">The authenticated CWT payload claims.</param>
    /// <param name="policy">The CBOR size and depth limits.</param>
    /// <returns>A successful result when every overlapping claim value is identical.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult ValidateAgainstPayload(
        CoseHeader header,
        CwtClaims payloadClaims,
        TokenPolicy policy)
    {
        if (payloadClaims == null)
        {
            throw new ArgumentNullException("payloadClaims");
        }

        CoseCwtClaimsResult headerClaims = Read(header, policy);
        if (!headerClaims.Success)
        {
            return ResultFailure(headerClaims.Error);
        }

        if (!headerClaims.Present)
        {
            return ResultSuccess();
        }

        int index = 0;
        while (index < (headerClaims.Claims ?? throw new InvalidOperationException("A required value is null.")).Values.Length)
        {
            CborMapEntry entry = headerClaims.Claims.Values[index];
            CborItem? payloadValue = Find(payloadClaims.Values, entry.Key);
            if (payloadValue != null
                && !Equivalent(entry.Value, payloadValue, policy))
            {
                return ResultFailure(
                    "cwt_header_claim_mismatch",
                    "A CWT claim in a COSE header differs from the same payload claim.");
            }

            index += 1;
        }

        return ResultSuccess();
    }

    static TokenResult Set(
        CoseHeader header,
        CwtClaims claims,
        TokenPolicy policy,
        bool protect)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenBytesResult encoded = claims.WriteWithPolicy(policy);
        if (!encoded.Success)
        {
            return ResultFailure(encoded.Error);
        }

        CborReadResult decoded = CborReader.Create((encoded.Bytes ?? throw new InvalidOperationException("A required value is null."))).Read();
        if (!decoded.Success)
        {
            return ResultFailure(decoded.Error);
        }

        CborItem? opposite = protect
            ? header.GetUnprotected(CoseHeaderLabels.CwtClaims)
            : header.GetProtected(CoseHeaderLabels.CwtClaims);
        if (opposite != null)
        {
            return ResultFailure(
                "cwt_claims_header_bucket_conflict",
                "The CWT Claims parameter already occurs in the other COSE header bucket.");
        }

        if (protect)
        {
            header.SetProtected(CoseHeaderLabels.CwtClaims, (decoded.Item ?? throw new InvalidOperationException("A required value is null.")));
        }
        else
        {
            header.SetUnprotected(CoseHeaderLabels.CwtClaims, (decoded.Item ?? throw new InvalidOperationException("A required value is null.")));
        }

        return ResultSuccess();
    }

    static CborItem? Find(CborMapEntry[] entries, CborItem? key)
    {
        if (entries == null)
        {
            return null;
        }

        int index = 0;
        while (index < entries.Length)
        {
            CborMapEntry entry = entries[index];
            if (entry != null
                && entry.Key != null
                && CborMapKeyComparer.AreEqual(entry.Key, (key ?? throw new InvalidOperationException("A required value is null."))))
            {
                return entry.Value;
            }

            index += 1;
        }

        return null;
    }

    static bool Equivalent(
        CborItem? left,
        CborItem right,
        TokenPolicy policy)
    {
        TokenBytesResult leftBytes = Encode(left, policy);
        TokenBytesResult rightBytes = Encode(right, policy);
        if (!leftBytes.Success
            || !rightBytes.Success
            || (leftBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length != (rightBytes.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            return false;
        }

        int index = 0;
        while (index < leftBytes.Bytes.Length)
        {
            if (leftBytes.Bytes[index] != rightBytes.Bytes[index])
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    static TokenBytesResult Encode(CborItem? item, TokenPolicy policy)
    {
        CborWriter writer = CborWriter.Create(
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        TokenResult written = writer.WriteDeterministicItem((item ?? throw new InvalidOperationException("A required value is null.")));
        TokenBytesResult result = new TokenBytesResult();
        if (!written.Success)
        {
            result.Error = written.Error;
            return result;
        }

        result.Success = true;
        result.Bytes = writer.ToArray();
        return result;
    }

    static CoseCwtClaimsResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static CoseCwtClaimsResult Failure(TokenError? error)
    {
        CoseCwtClaimsResult result = new CoseCwtClaimsResult();
        result.Error = error;
        return result;
    }

    static TokenResult ResultSuccess()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult ResultFailure(string code, string message)
    {
        return ResultFailure(new TokenError(code, message, -1));
    }

    static TokenResult ResultFailure(TokenError? error)
    {
        TokenResult result = new TokenResult();
        result.Error = error;
        return result;
    }
}
