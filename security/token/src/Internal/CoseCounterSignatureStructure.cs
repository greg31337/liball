using System;
namespace token;

/// <summary>Constructs deterministic RFC 9338 Countersign_structure values.</summary>
internal static class CoseCounterSignatureStructure
{
    /// <summary>Constructs a full version 2 countersignature input with default limits.</summary>
    /// <param name="bodyProtected">The exact protected bytes of the target.</param>
    /// <param name="signerProtected">The exact protected bytes of the countersigner.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="payload">The target payload, ciphertext, or signature bytes.</param>
    /// <param name="otherFields">Additional target byte-string fields in serialization order.</param>
    /// <returns>The encoded Countersign_structure or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Create(
        byte[] bodyProtected,
        byte[] signerProtected,
        byte[] externalAuthenticatedData,
        byte[] payload,
        byte[][] otherFields)
    {
        return Create(
            bodyProtected,
            signerProtected,
            externalAuthenticatedData,
            payload,
            otherFields,
            new TokenPolicy());
    }

    /// <summary>Constructs a full version 2 countersignature input.</summary>
    /// <param name="bodyProtected">The exact protected bytes of the target.</param>
    /// <param name="signerProtected">The exact protected bytes of the countersigner.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="payload">The target payload, ciphertext, or signature bytes.</param>
    /// <param name="otherFields">Additional target byte-string fields in serialization order.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded Countersign_structure or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Create(
        byte[] bodyProtected,
        byte[] signerProtected,
        byte[] externalAuthenticatedData,
        byte[] payload,
        byte[][] otherFields,
        TokenPolicy policy)
    {
        return CreateCore(
            false,
            bodyProtected,
            signerProtected,
            externalAuthenticatedData,
            payload,
            otherFields,
            policy);
    }

    /// <summary>Constructs an abbreviated version 2 countersignature input with default limits.</summary>
    /// <param name="bodyProtected">The exact protected bytes of the target.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="payload">The target payload, ciphertext, or signature bytes.</param>
    /// <param name="otherFields">Additional target byte-string fields in serialization order.</param>
    /// <returns>The encoded Countersign_structure or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateAbbreviated(
        byte[] bodyProtected,
        byte[] externalAuthenticatedData,
        byte[] payload,
        byte[][] otherFields)
    {
        return CreateAbbreviated(
            bodyProtected,
            externalAuthenticatedData,
            payload,
            otherFields,
            new TokenPolicy());
    }

    /// <summary>Constructs a size-limited abbreviated version 2 countersignature input.</summary>
    /// <param name="bodyProtected">The exact protected bytes of the target.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes.</param>
    /// <param name="payload">The target payload, ciphertext, or signature bytes.</param>
    /// <param name="otherFields">Additional target byte-string fields in serialization order.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded Countersign_structure or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateAbbreviated(
        byte[] bodyProtected,
        byte[] externalAuthenticatedData,
        byte[] payload,
        byte[][] otherFields,
        TokenPolicy policy)
    {
        return CreateCore(
            true,
            bodyProtected,
            null,
            externalAuthenticatedData,
            payload,
            otherFields,
            policy);
    }

    static TokenBytesResult CreateCore(
        bool abbreviated,
        byte[] bodyProtected,
        byte[]? signerProtected,
        byte[] externalAuthenticatedData,
        byte[] payload,
        byte[][] otherFields,
        TokenPolicy policy)
    {
        if (bodyProtected == null)
        {
            throw new ArgumentNullException("bodyProtected");
        }

        if (!abbreviated && signerProtected == null)
        {
            throw new ArgumentNullException("signerProtected");
        }

        if (externalAuthenticatedData == null)
        {
            throw new ArgumentNullException("externalAuthenticatedData");
        }

        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        if (otherFields == null)
        {
            throw new ArgumentNullException("otherFields");
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
                "The countersignature structure policy contains an invalid size or depth limit.");
        }

        int otherIndex = 0;
        while (otherIndex < otherFields.Length)
        {
            if (otherFields[otherIndex] == null)
            {
                return Failure(
                    "invalid_countersignature_other_field",
                    "Every countersignature other field must contain bytes.");
            }

            otherIndex += 1;
        }

        bool hasOtherFields = otherFields.Length > 0;
        int length = abbreviated ? 4 : 5;
        if (hasOtherFields)
        {
            length += 1;
        }

        CborItem[] fields = new CborItem[length];
        fields[0] = TextItem(Context(abbreviated, hasOtherFields));
        fields[1] = BytesItem(bodyProtected);
        int index = 2;
        if (!abbreviated)
        {
            fields[index] = BytesItem(signerProtected);
            index += 1;
        }

        fields[index] = BytesItem(externalAuthenticatedData);
        fields[index + 1] = BytesItem(payload);
        if (hasOtherFields)
        {
            CborItem[] values = new CborItem[otherFields.Length];
            otherIndex = 0;
            while (otherIndex < otherFields.Length)
            {
                values[otherIndex] = BytesItem(otherFields[otherIndex]);
                otherIndex += 1;
            }

            fields[index + 2] = ArrayItem(values);
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

    static string Context(bool abbreviated, bool hasOtherFields)
    {
        if (abbreviated)
        {
            return hasOtherFields
                ? "CounterSignature0V2"
                : "CounterSignature0";
        }

        return hasOtherFields
            ? "CounterSignatureV2"
            : "CounterSignature";
    }

    static CborItem TextItem(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static CborItem BytesItem(byte[]? value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static CborItem ArrayItem(CborItem[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = value;
        return item;
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
