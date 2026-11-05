using System;
namespace token;

/// <summary>Represents an RFC 9338 full version 2 COSE countersignature.</summary>
public class CoseCounterSignature
{
    const ulong CounterSignatureTag = 19;

    /// <summary>Contains the countersigner protected and unprotected headers.</summary>
    public CoseHeader? Header;

    /// <summary>Contains the countersignature bytes.</summary>
    public byte[]? Signature;

    /// <summary>Indicates whether standalone encoding uses CBOR tag 19.</summary>
    public bool Tagged;

    /// <summary>Indicates whether the countersignature was cryptographically verified.</summary>
    public bool SignatureVerified;

    /// <summary>Initializes an empty tagged countersignature.</summary>
    public CoseCounterSignature()
    {
        Header = new CoseHeader();
        Signature = Array.Empty<byte>();
        Tagged = true;
        SignatureVerified = false;
    }

    /// <summary>Reads a tagged or contextually untagged countersignature with default limits.</summary>
    /// <param name="cbor">The encoded countersignature.</param>
    /// <returns>The parsed countersignature or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>cbor</c> is null.</exception>
    internal static CoseCounterSignatureResult Read(byte[] cbor)
    {
        return Read(cbor, new TokenPolicy());
    }

    /// <summary>Reads a tagged or contextually untagged countersignature.</summary>
    /// <param name="cbor">The encoded countersignature.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>The parsed countersignature or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseCounterSignatureResult Read(
        byte[] cbor,
        TokenPolicy policy)
    {
        if (cbor == null)
        {
            throw new ArgumentNullException("cbor");
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
                "The countersignature policy contains an invalid size or depth limit.");
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
                "trailing_countersignature_data",
                "A countersignature must contain exactly one CBOR item.");
        }

        CborItem? item = decoded.Item;
        bool tagged = (item ?? throw new InvalidOperationException("A required value is null.")).Kind == TokenCborValueKind.Tag;
        if (tagged)
        {
            if (item.TagValue != CounterSignatureTag)
            {
                return Failure(
                    "unexpected_countersignature_tag",
                    "A tagged countersignature must use CBOR tag 19.");
            }

            item = item.TaggedValue;
        }

        CoseCounterSignatureResult result = FromItem(item, policy);
        if (result.Success)
        {
            (result.CounterSignature ?? throw new InvalidOperationException("A required value is null.")).Tagged = tagged;
        }

        return result;
    }

    /// <summary>Reads version 2 full countersignatures from unprotected header label 11.</summary>
    /// <param name="header">The containing COSE header.</param>
    /// <returns>The parsed countersignatures or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>header</c> is null.</exception>
    internal static CoseCounterSignatureCollectionResult ReadHeader(
        CoseHeader header)
    {
        return ReadHeader(header, new TokenPolicy());
    }

    /// <summary>Reads version 2 full countersignatures from unprotected header label 11 with explicit limits.</summary>
    /// <param name="header">The containing COSE header.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>The parsed countersignatures or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseCounterSignatureCollectionResult ReadHeader(
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

        if (header.GetProtected(CoseHeaderLabels.CounterSignatureV2) != null)
        {
            return CollectionFailure(
                "countersignature_must_be_unprotected",
                "A version 2 countersignature must be an unprotected header parameter.");
        }

        CborItem? value = header.GetUnprotected(
            CoseHeaderLabels.CounterSignatureV2);
        if (value == null)
        {
            return CollectionFailure(
                "missing_countersignature",
                "The COSE header does not contain a version 2 countersignature.");
        }

        CborItem[] items;
        if (IsSignatureItem(value))
        {
            items = new CborItem[] { value };
        }
        else if (value.Kind == TokenCborValueKind.Array
            && value.ArrayValue != null
            && value.ArrayValue.Length > 0)
        {
            items = value.ArrayValue;
        }
        else
        {
            return CollectionFailure(
                "invalid_countersignature_header",
                "Header label 11 must contain a countersignature or a nonempty array of countersignatures.");
        }

        CoseCounterSignature[] signatures =
            new CoseCounterSignature[items.Length];
        int index = 0;
        while (index < items.Length)
        {
            CoseCounterSignatureResult parsed = FromItem(
                items[index],
                policy);
            if (!parsed.Success)
            {
                return CollectionFailure(parsed.Error);
            }

            (parsed.CounterSignature ?? throw new InvalidOperationException("A required value is null.")).Tagged = false;
            signatures[index] = parsed.CounterSignature;
            index += 1;
        }

        CoseCounterSignatureCollectionResult result =
            new CoseCounterSignatureCollectionResult();
        result.Success = true;
        result.CounterSignatures = signatures;
        return result;
    }

    /// <summary>Writes this countersignature using its tagged setting.</summary>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded countersignature or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>policy</c> is null.</exception>
    internal TokenBytesResult WriteWithPolicy(TokenPolicy policy)
    {
        return Write(this, Tagged, policy);
    }

    /// <summary>Writes this countersignature using its tagged setting and default limits.</summary>
    /// <returns>The encoded countersignature or stable failure details.</returns>
    internal TokenBytesResult Write()
    {
        return Write(this, Tagged, new TokenPolicy());
    }

    /// <summary>Writes this countersignature with explicit tag control and default limits.</summary>
    /// <param name="tagged">Whether to include CBOR tag 19.</param>
    /// <returns>The encoded countersignature or stable failure details.</returns>
    internal TokenBytesResult WriteWithTag(bool tagged)
    {
        return Write(this, tagged, new TokenPolicy());
    }

    /// <summary>Writes a standalone full countersignature.</summary>
    /// <param name="counterSignature">The countersignature to write.</param>
    /// <param name="tagged">Whether to include CBOR tag 19.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded countersignature or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Write(
        CoseCounterSignature counterSignature,
        bool tagged,
        TokenPolicy policy)
    {
        if (counterSignature == null)
        {
            throw new ArgumentNullException("counterSignature");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        CoseCounterSignatureItemResult encoded = ToItem(
            counterSignature,
            policy);
        if (!encoded.Success)
        {
            return BytesFailure(encoded.Error);
        }

        CborItem? output = tagged
            ? TagItem(CounterSignatureTag, encoded.Item)
            : encoded.Item;
        CborWriter writer = CborWriter.Create(
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        TokenResult written = writer.WriteDeterministicItem((output ?? throw new InvalidOperationException("A required value is null.")));
        if (!written.Success)
        {
            return BytesFailure(written.Error);
        }

        counterSignature.Tagged = tagged;
        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = writer.ToArray();
        return result;
    }

    /// <summary>Stores one or more full countersignatures in unprotected header label 11.</summary>
    /// <param name="header">The containing COSE header.</param>
    /// <param name="counterSignatures">The countersignatures to store.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>A success result or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult SetHeader(
        CoseHeader header,
        CoseCounterSignature[] counterSignatures,
        TokenPolicy policy)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        if (counterSignatures == null)
        {
            throw new ArgumentNullException("counterSignatures");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        if (counterSignatures.Length == 0)
        {
            return ValidationFailure(
                "missing_countersignature",
                "At least one countersignature is required.");
        }

        if (header.GetProtected(CoseHeaderLabels.CounterSignatureV2) != null)
        {
            return ValidationFailure(
                "countersignature_must_be_unprotected",
                "A version 2 countersignature must be an unprotected header parameter.");
        }

        CborItem[] items = new CborItem[counterSignatures.Length];
        int index = 0;
        while (index < counterSignatures.Length)
        {
            if (counterSignatures[index] == null)
            {
                return ValidationFailure(
                    "invalid_countersignature",
                    "A countersignature entry is missing.");
            }

            CoseCounterSignatureItemResult encoded = ToItem(
                counterSignatures[index],
                policy);
            if (!encoded.Success)
            {
                return ValidationFailure(encoded.Error);
            }

            items[index] = (encoded.Item ?? throw new InvalidOperationException("A required value is null."));
            index += 1;
        }

        CborItem value = items.Length == 1
            ? items[0]
            : ArrayItem(items);
        header.SetUnprotected(
            CoseHeaderLabels.CounterSignatureV2,
            value);
        return ValidationSuccess();
    }

    internal static CoseCounterSignatureResult FromItem(
        CborItem? item,
        TokenPolicy policy)
    {
        if (!IsSignatureItem(item))
        {
            return Failure(
                "invalid_countersignature_structure",
                "A full countersignature must be a three-item CBOR array.");
        }

        CborItem[]? fields = (item ?? throw new InvalidOperationException("A required value is null.")).ArrayValue;
        CoseHeaderResult header = CoseHeader.FromBuckets(
            (fields ?? throw new InvalidOperationException("A required value is null."))[0],
            fields[1],
            policy);
        if (!header.Success)
        {
            return Failure(header.Error);
        }

        CoseCounterSignature counterSignature =
            new CoseCounterSignature();
        counterSignature.Header = header.Header;
        counterSignature.Signature = Copy(fields[2].Bytes);
        CoseCounterSignatureResult result =
            new CoseCounterSignatureResult();
        result.Success = true;
        result.CounterSignature = counterSignature;
        return result;
    }

    internal static CoseCounterSignatureItemResult ToItem(
        CoseCounterSignature counterSignature,
        TokenPolicy policy)
    {
        if (counterSignature.Header == null
            || counterSignature.Signature == null)
        {
            return ItemFailure(
                "invalid_countersignature",
                "A countersignature requires headers and signature bytes.");
        }

        TokenBytesResult protectedBytes = CoseSign.GetProtectedBytes(
            counterSignature.Header,
            policy);
        if (!protectedBytes.Success)
        {
            return ItemFailure(protectedBytes.Error);
        }

        counterSignature.Header.ProtectedBytes = Copy(protectedBytes.Bytes);
        CoseCounterSignatureItemResult result =
            new CoseCounterSignatureItemResult();
        result.Success = true;
        result.Item = ArrayItem(new CborItem[]
        {
            BytesItem(protectedBytes.Bytes),
            MapItem(counterSignature.Header.UnprotectedParameters),
            BytesItem(counterSignature.Signature)
        });
        return result;
    }

    internal static bool IsSignatureItem(CborItem? item)
    {
        return item != null
            && item.Kind == TokenCborValueKind.Array
            && item.ArrayValue != null
            && item.ArrayValue.Length == 3
            && item.ArrayValue[0] != null
            && item.ArrayValue[0].Kind == TokenCborValueKind.ByteString
            && item.ArrayValue[1] != null
            && item.ArrayValue[1].Kind == TokenCborValueKind.Map
            && item.ArrayValue[2] != null
            && item.ArrayValue[2].Kind == TokenCborValueKind.ByteString;
    }

    internal static byte[] Copy(byte[]? value)
    {
        return value.AsSpan().ToArray();
    }

    internal static CborItem BytesItem(byte[]? value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    internal static CborItem MapItem(CborMapEntry[]? value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Map;
        item.MapValue = value;
        return item;
    }

    internal static CborItem ArrayItem(CborItem[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = value;
        return item;
    }

    internal static CborItem TagItem(ulong tag, CborItem? value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Tag;
        item.TagValue = tag;
        item.TaggedValue = value;
        return item;
    }

    internal static CoseCounterSignatureResult Failure(
        string code,
        string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    internal static CoseCounterSignatureResult Failure(TokenError? error)
    {
        CoseCounterSignatureResult result =
            new CoseCounterSignatureResult();
        result.Error = error;
        return result;
    }

    internal static CoseCounterSignatureCollectionResult CollectionFailure(
        string code,
        string message)
    {
        return CollectionFailure(new TokenError(code, message, -1));
    }

    internal static CoseCounterSignatureCollectionResult CollectionFailure(
        TokenError? error)
    {
        CoseCounterSignatureCollectionResult result =
            new CoseCounterSignatureCollectionResult();
        result.Error = error;
        return result;
    }

    internal static CoseCounterSignatureItemResult ItemFailure(
        string code,
        string message)
    {
        return ItemFailure(new TokenError(code, message, -1));
    }

    internal static CoseCounterSignatureItemResult ItemFailure(TokenError? error)
    {
        CoseCounterSignatureItemResult result =
            new CoseCounterSignatureItemResult();
        result.Error = error;
        return result;
    }

    internal static TokenBytesResult BytesFailure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }

    internal static TokenResult ValidationSuccess()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    internal static TokenResult ValidationFailure(string code, string message)
    {
        return ValidationFailure(new TokenError(code, message, -1));
    }

    internal static TokenResult ValidationFailure(TokenError? error)
    {
        TokenResult result = new TokenResult();
        result.Error = error;
        return result;
    }
}
