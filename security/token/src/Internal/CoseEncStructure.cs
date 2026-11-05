using System;
namespace token;

/// <summary>Constructs deterministic RFC 9052 Enc_structure values.</summary>
internal static class CoseEncStructure
{
    /// <summary>Constructs an Enc_structure with empty external authenticated data.</summary>
    /// <param name="context">The encryption operation context.</param>
    /// <param name="protectedParameters">The exact encoded protected header map, or an empty array.</param>
    /// <returns>The encoded Enc_structure, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>protectedParameters</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>context</c> is not defined.</exception>
    internal static TokenBytesResult Create(
        CoseEncStructureContext context,
        byte[] protectedParameters)
    {
        return Create(
            context,
            protectedParameters,
            Array.Empty<byte>(),
            new TokenPolicy());
    }

    /// <summary>Constructs an Enc_structure with external authenticated data.</summary>
    /// <param name="context">The encryption operation context.</param>
    /// <param name="protectedParameters">The exact encoded protected header map, or an empty array.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes, or an empty array.</param>
    /// <returns>The encoded Enc_structure, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an array argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>context</c> is not defined.</exception>
    internal static TokenBytesResult Create(
        CoseEncStructureContext context,
        byte[] protectedParameters,
        byte[] externalAuthenticatedData)
    {
        return Create(
            context,
            protectedParameters,
            externalAuthenticatedData,
            new TokenPolicy());
    }

    /// <summary>Constructs a size-limited Enc_structure with external authenticated data.</summary>
    /// <param name="context">The encryption operation context.</param>
    /// <param name="protectedParameters">The exact encoded protected header map, or an empty array.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes, or an empty array.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded Enc_structure, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an array or policy argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>context</c> is not defined.</exception>
    internal static TokenBytesResult Create(
        CoseEncStructureContext context,
        byte[] protectedParameters,
        byte[] externalAuthenticatedData,
        TokenPolicy policy)
    {
        if (!IsDefined(context))
        {
            throw new ArgumentOutOfRangeException("context");
        }

        if (protectedParameters == null)
        {
            throw new ArgumentNullException("protectedParameters");
        }

        if (externalAuthenticatedData == null)
        {
            throw new ArgumentNullException("externalAuthenticatedData");
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
                "The COSE Enc_structure policy contains an invalid size or depth limit.");
        }

        CborItem[] fields = new CborItem[3];
        fields[0] = TextString(ContextText(context));
        fields[1] = ByteString(protectedParameters);
        fields[2] = ByteString(externalAuthenticatedData);

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

    static string ContextText(CoseEncStructureContext context)
    {
        if (context == CoseEncStructureContext.Encrypt)
        {
            return "Encrypt";
        }

        if (context == CoseEncStructureContext.Encrypt0)
        {
            return "Encrypt0";
        }

        if (context == CoseEncStructureContext.EncryptRecipient)
        {
            return "Enc_Recipient";
        }

        if (context == CoseEncStructureContext.MacRecipient)
        {
            return "Mac_Recipient";
        }

        return "Rec_Recipient";
    }

    static bool IsDefined(CoseEncStructureContext context)
    {
        return context == CoseEncStructureContext.Encrypt
            || context == CoseEncStructureContext.Encrypt0
            || context == CoseEncStructureContext.EncryptRecipient
            || context == CoseEncStructureContext.MacRecipient
            || context == CoseEncStructureContext.Recipient;
    }

    static CborItem TextString(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static CborItem ByteString(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
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
