using System;
namespace token;

/// <summary>Constructs deterministic RFC 9053 COSE_KDF_Context values.</summary>
internal static class CoseKdfContext
{
    /// <summary>Constructs a COSE KDF context using default output limits.</summary>
    /// <param name="parameters">The algorithm, party, and supplemental context inputs.</param>
    /// <returns>The encoded COSE_KDF_Context, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>parameters</c> is null.</exception>
    internal static TokenBytesResult Create(
        CoseKdfContextParameters parameters)
    {
        return Create(parameters, new TokenPolicy());
    }

    /// <summary>Constructs a size-limited COSE KDF context.</summary>
    /// <param name="parameters">The algorithm, party, and supplemental context inputs.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded COSE_KDF_Context, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Create(
        CoseKdfContextParameters parameters,
        TokenPolicy policy)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException("parameters");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumCborDepth <= 0)
        {
            return Failure(
                "invalid_policy",
                "The COSE KDF context policy contains an invalid size or depth limit.");
        }

        TokenResult validation = Validate(parameters);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        int outerLength = parameters.SupplementalPrivateInfo == null
            ? 4
            : 5;
        int publicLength = parameters.SupplementalPublicOther == null
            ? 2
            : 3;
        CborItem[] supplementalPublic = new CborItem[publicLength];
        supplementalPublic[0] = Unsigned(parameters.KeyDataLength);
        supplementalPublic[1] = ByteString(parameters.ProtectedParameters);
        if (parameters.SupplementalPublicOther != null)
        {
            supplementalPublic[2] = ByteString(
                parameters.SupplementalPublicOther);
        }

        CborItem[] fields = new CborItem[outerLength];
        fields[0] = parameters.AlgorithmIdentifier;
        fields[1] = PartyInfo(parameters.PartyUInfo);
        fields[2] = PartyInfo(parameters.PartyVInfo);
        fields[3] = ArrayItem(supplementalPublic);
        if (parameters.SupplementalPrivateInfo != null)
        {
            fields[4] = ByteString(parameters.SupplementalPrivateInfo);
        }

        CborWriter writer = CborWriter.Create(
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        TokenResult written = writer.WriteArray(fields);
        if (!written.Success)
        {
            return Failure(written.Error);
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = writer.ToArray();
        return result;
    }

    static TokenResult Validate(CoseKdfContextParameters parameters)
    {
        if (!IsAlgorithmIdentifier(parameters.AlgorithmIdentifier))
        {
            return ValidationFailure(
                "invalid_cose_kdf_algorithm",
                "The COSE KDF algorithm identifier must be an integer or text string.");
        }

        if (parameters.PartyUInfo == null
            || parameters.PartyVInfo == null
            || parameters.ProtectedParameters == null)
        {
            return ValidationFailure(
                "invalid_cose_kdf_context",
                "The COSE KDF context is missing required information.");
        }

        if (!IsNonce(parameters.PartyUInfo.Nonce)
            || !IsNonce(parameters.PartyVInfo.Nonce))
        {
            return ValidationFailure(
                "invalid_cose_kdf_nonce",
                "A COSE KDF party nonce must be a byte string, integer, or null.");
        }

        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static bool IsAlgorithmIdentifier(CborItem item)
    {
        return item != null
            && (item.Kind == TokenCborValueKind.UnsignedInteger
                || item.Kind == TokenCborValueKind.NegativeInteger
                || item.Kind == TokenCborValueKind.TextString
                    && item.Text != null);
    }

    static bool IsNonce(CborItem? item)
    {
        if (item == null)
        {
            return true;
        }

        if (item.Kind == TokenCborValueKind.UnsignedInteger
            || item.Kind == TokenCborValueKind.NegativeInteger
            || item.Kind == TokenCborValueKind.Null)
        {
            return true;
        }

        return item.Kind == TokenCborValueKind.ByteString
            && item.Bytes != null;
    }

    static CborItem PartyInfo(CoseKdfPartyInfo party)
    {
        CborItem[] fields = new CborItem[3];
        fields[0] = party.Identity == null
            ? NullItem()
            : ByteString(party.Identity);
        fields[1] = party.Nonce == null
            ? NullItem()
            : party.Nonce;
        fields[2] = party.Other == null
            ? NullItem()
            : ByteString(party.Other);
        return ArrayItem(fields);
    }

    static CborItem Unsigned(ulong value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.UnsignedInteger;
        item.IntegerArgument = value;
        return item;
    }

    static CborItem ByteString(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static CborItem NullItem()
    {
        return new CborItem();
    }

    static CborItem ArrayItem(CborItem[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = value;
        return item;
    }

    static TokenResult ValidationFailure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }

    static TokenBytesResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static TokenBytesResult Failure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }
}
