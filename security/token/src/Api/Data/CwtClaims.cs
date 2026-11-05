using System;
namespace token;

/// <summary>Represents registered and private CWT claims as a CBOR map.</summary>
public class CwtClaims
{
    /// <summary>Contains claims in their current model order.</summary>
    public CborMapEntry[] Values;

    /// <summary>Initializes an empty CWT Claims Set.</summary>
    public CwtClaims()
    {
        Values = Array.Empty<CborMapEntry>();
    }

    /// <summary>Creates a CWT Claims Set from existing entries.</summary>
    /// <param name="values">The claim entries to copy.</param>
    /// <returns>A claim set containing an independent copy of the entries.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    internal static CwtClaims Create(CborMapEntry[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        CwtClaims result = new CwtClaims();
        result.Values = values.AsSpan().ToArray();
        return result;
    }

    /// <summary>Reads a CWT Claims Set with default limits.</summary>
    /// <param name="cbor">The encoded CBOR map.</param>
    /// <returns>The parsed claims or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>cbor</c> is null.</exception>
    internal static CwtClaimsResult Read(byte[] cbor)
    {
        return Read(cbor, new TokenPolicy());
    }

    /// <summary>Reads a CWT Claims Set with explicit size and depth limits.</summary>
    /// <param name="cbor">The encoded CBOR map.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>The parsed claims or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CwtClaimsResult Read(byte[] cbor, TokenPolicy policy)
    {
        if (cbor == null)
        {
            throw new ArgumentNullException("cbor");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return Failure(policyResult.Error);
        }

        CborReader reader = CborReader.Create(
            cbor,
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        CborReadResult decoded = reader.Read();
        if (!decoded.Success)
        {
            return Failure(decoded.Error);
        }

        if (reader.GetRemainingLength() != 0)
        {
            return Failure(
                "trailing_cwt_claim_data",
                "A CWT Claims Set must contain exactly one CBOR item.");
        }

        if ((decoded.Item ?? throw new InvalidOperationException("A required value is null.")).Kind != TokenCborValueKind.Map
            || decoded.Item.MapValue == null)
        {
            return Failure(
                "cwt_claims_not_map",
                "A CWT Claims Set must be a CBOR map.");
        }

        return FromMap(decoded.Item.MapValue, policy);
    }

    /// <summary>Creates a validated claim set from CBOR map entries using default limits.</summary>
    /// <param name="values">The claim entries to validate and copy.</param>
    /// <returns>The claims or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    internal static CwtClaimsResult FromMap(CborMapEntry[] values)
    {
        return FromMap(values, new TokenPolicy());
    }

    /// <summary>Creates a validated claim set from CBOR map entries.</summary>
    /// <param name="values">The claim entries to validate and copy.</param>
    /// <param name="policy">The CBOR depth limit to enforce.</param>
    /// <returns>The claims or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CwtClaimsResult FromMap(
        CborMapEntry[] values,
        TokenPolicy policy)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return Failure(policyResult.Error);
        }

        TokenResult validation = ValidateValues(
            values,
            policy.MaximumCborDepth);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        CwtClaimsResult result = new CwtClaimsResult();
        result.Success = true;
        result.Claims = CwtClaims.Create(values);
        return result;
    }

    /// <summary>Finds an integer-labeled claim.</summary>
    /// <param name="label">The registered or private integer claim label.</param>
    /// <returns>The matching value, or <c>null</c> when absent.</returns>
    internal CborItem? Get(int label)
    {
        return GetCore(IntegerLabel(label));
    }

    /// <summary>Finds a text-labeled private claim.</summary>
    /// <param name="label">The case-sensitive private claim label.</param>
    /// <returns>The matching value, or <c>null</c> when absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>label</c> is null.</exception>
    internal CborItem? GetByTextLabel(string label)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        return GetCore(TextLabel(label));
    }

    /// <summary>Adds or replaces an integer-labeled claim.</summary>
    /// <param name="label">The registered or private integer claim label.</param>
    /// <param name="value">The CBOR value to store.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    internal void Set(int label, CborItem value)
    {
        SetCore(IntegerLabel(label), value);
    }

    /// <summary>Adds or replaces a text-labeled private claim.</summary>
    /// <param name="label">The case-sensitive private claim label.</param>
    /// <param name="value">The CBOR value to store.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal void SetByTextLabel(string label, CborItem value)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        SetCore(TextLabel(label), value);
    }

    /// <summary>Removes an integer-labeled claim.</summary>
    /// <param name="label">The claim label to remove.</param>
    /// <returns><c>true</c> when a claim was removed.</returns>
    internal bool Remove(int label)
    {
        return RemoveCore(IntegerLabel(label));
    }

    /// <summary>Removes a text-labeled private claim.</summary>
    /// <param name="label">The case-sensitive claim label to remove.</param>
    /// <returns><c>true</c> when a claim was removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>label</c> is null.</exception>
    internal bool RemoveByTextLabel(string label)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        return RemoveCore(TextLabel(label));
    }

    /// <summary>Writes this claim set as deterministic CBOR with default limits.</summary>
    /// <returns>The encoded CBOR map or stable failure details.</returns>
    internal TokenBytesResult Write()
    {
        return Write(this, new TokenPolicy());
    }

    /// <summary>Writes this claim set as deterministic CBOR with explicit limits.</summary>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded CBOR map or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>policy</c> is null.</exception>
    internal TokenBytesResult WriteWithPolicy(TokenPolicy policy)
    {
        return Write(this, policy);
    }

    /// <summary>Writes a claim set as deterministic CBOR with default limits.</summary>
    /// <param name="claims">The claims to write.</param>
    /// <returns>The encoded CBOR map or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>claims</c> is null.</exception>
    internal static TokenBytesResult Write(CwtClaims claims)
    {
        return Write(claims, new TokenPolicy());
    }

    /// <summary>Writes a claim set as deterministic CBOR with explicit limits.</summary>
    /// <param name="claims">The claims to write.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded CBOR map or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Write(
        CwtClaims claims,
        TokenPolicy policy)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return BytesFailure(policyResult.Error);
        }

        if (claims.Values == null)
        {
            return BytesFailure(
                "invalid_cwt_claims",
                "A CWT Claims Set requires a claim-entry array.");
        }

        TokenResult validation = ValidateValues(
            claims.Values,
            policy.MaximumCborDepth);
        if (!validation.Success)
        {
            return BytesFailure(validation.Error);
        }

        CborWriter writer = CborWriter.Create(
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        TokenResult written = writer.WriteDeterministicMap(claims.Values);
        if (!written.Success)
        {
            return BytesFailure(written.Error);
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = writer.ToArray();
        return result;
    }

    CborItem? GetCore(CborItem key)
    {
        if (Values == null)
        {
            return null;
        }

        int index = 0;
        while (index < Values.Length)
        {
            CborMapEntry entry = Values[index];
            if (entry != null
                && entry.Key != null
                && CborMapKeyComparer.AreEqual(entry.Key, key))
            {
                return entry.Value;
            }

            index += 1;
        }

        return null;
    }

    void SetCore(CborItem key, CborItem value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (Values == null)
        {
            Values = Array.Empty<CborMapEntry>();
        }

        int index = 0;
        while (index < Values.Length)
        {
            CborMapEntry entry = Values[index];
            if (entry != null
                && entry.Key != null
                && CborMapKeyComparer.AreEqual(entry.Key, key))
            {
                entry.Value = value;
                return;
            }

            index += 1;
        }

        CborMapEntry[] expanded = new CborMapEntry[Values.Length + 1];
        Values.AsSpan().CopyTo(expanded);
        expanded[expanded.Length - 1] = new CborMapEntry(key, value);
        Values = expanded;
    }

    bool RemoveCore(CborItem key)
    {
        if (Values == null)
        {
            return false;
        }

        int removed = -1;
        int index = 0;
        while (index < Values.Length)
        {
            CborMapEntry entry = Values[index];
            if (entry != null
                && entry.Key != null
                && CborMapKeyComparer.AreEqual(entry.Key, key))
            {
                removed = index;
                break;
            }

            index += 1;
        }

        if (removed < 0)
        {
            return false;
        }

        CborMapEntry[] reduced = new CborMapEntry[Values.Length - 1];
        index = 0;
        int destination = 0;
        while (index < Values.Length)
        {
            if (index != removed)
            {
                reduced[destination] = Values[index];
                destination += 1;
            }

            index += 1;
        }

        Values = reduced;
        return true;
    }

    internal static TokenResult ValidateValues(
        CborMapEntry[] values,
        int maximumDepth)
    {
        TokenResult map = CborMapKeyComparer.Validate(
            values,
            maximumDepth);
        if (!map.Success)
        {
            return map;
        }

        int index = 0;
        while (index < values.Length)
        {
            CborItem? key = values[index].Key;
            if ((key ?? throw new InvalidOperationException("A required value is null.")).Kind != TokenCborValueKind.UnsignedInteger
                && key.Kind != TokenCborValueKind.NegativeInteger
                && key.Kind != TokenCborValueKind.TextString)
            {
                return ValidationFailure(
                    "invalid_cwt_claim_key",
                    "A CWT claim key must be an integer or text string.");
            }

            if (key.Kind == TokenCborValueKind.TextString
                && key.Text == null)
            {
                return ValidationFailure(
                    "invalid_cwt_claim_key",
                    "A text CWT claim key must contain text.");
            }

            index += 1;
        }

        return ValidationSuccess();
    }

    internal static TokenResult ValidatePolicy(TokenPolicy policy)
    {
        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumCborDepth <= 0)
        {
            return ValidationFailure(
                "invalid_policy",
                "The CWT Claims Set policy contains an invalid size or depth limit.");
        }

        return ValidationSuccess();
    }

    internal static CborItem IntegerLabel(int value)
    {
        CborItem item = new CborItem();
        if (value >= 0)
        {
            item.Kind = TokenCborValueKind.UnsignedInteger;
            item.IntegerArgument = (ulong)value;
        }
        else
        {
            item.Kind = TokenCborValueKind.NegativeInteger;
            item.IntegerArgument = (ulong)(-(long)value - 1L);
        }

        return item;
    }

    internal static CborItem TextLabel(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    internal static TokenResult ValidationSuccess()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    internal static TokenResult ValidationFailure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }

    internal static CwtClaimsResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    internal static CwtClaimsResult Failure(TokenError? error)
    {
        CwtClaimsResult result = new CwtClaimsResult();
        result.Error = error;
        return result;
    }

    internal static TokenBytesResult BytesFailure(string code, string message)
    {
        return BytesFailure(new TokenError(code, message, -1));
    }

    internal static TokenBytesResult BytesFailure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }
}
