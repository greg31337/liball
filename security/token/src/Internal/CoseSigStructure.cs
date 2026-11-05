using System;
namespace token;

/// <summary>Constructs deterministic RFC 9052 Sig_structure values.</summary>
internal static class CoseSigStructure
{
    /// <summary>Constructs a COSE_Sign1 Sig_structure with empty external authenticated data.</summary>
    /// <param name="bodyProtected">The exact encoded protected body header map, or an empty array.</param>
    /// <param name="payload">The complete attached or detached payload bytes.</param>
    /// <returns>The encoded Sig_structure, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateSignature1(
        byte[] bodyProtected,
        byte[] payload)
    {
        return CreateSignature1(
            bodyProtected,
            Array.Empty<byte>(),
            payload,
            new TokenPolicy());
    }

    /// <summary>Constructs a COSE_Sign1 Sig_structure with external authenticated data.</summary>
    /// <param name="bodyProtected">The exact encoded protected body header map, or an empty array.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes, or an empty array.</param>
    /// <param name="payload">The complete attached or detached payload bytes.</param>
    /// <returns>The encoded Sig_structure, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateSignature1(
        byte[] bodyProtected,
        byte[] externalAuthenticatedData,
        byte[] payload)
    {
        return CreateSignature1(
            bodyProtected,
            externalAuthenticatedData,
            payload,
            new TokenPolicy());
    }

    /// <summary>Constructs a size-limited COSE_Sign1 Sig_structure.</summary>
    /// <param name="bodyProtected">The exact encoded protected body header map, or an empty array.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes, or an empty array.</param>
    /// <param name="payload">The complete attached or detached payload bytes.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded Sig_structure, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateSignature1(
        byte[] bodyProtected,
        byte[] externalAuthenticatedData,
        byte[] payload,
        TokenPolicy policy)
    {
        if (bodyProtected == null)
        {
            throw new ArgumentNullException("bodyProtected");
        }

        if (externalAuthenticatedData == null)
        {
            throw new ArgumentNullException("externalAuthenticatedData");
        }

        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        return Create(
            "Signature1",
            bodyProtected,
            null,
            externalAuthenticatedData,
            payload,
            policy);
    }

    /// <summary>Constructs a COSE_Sign Sig_structure with empty external authenticated data.</summary>
    /// <param name="bodyProtected">The exact encoded protected body header map, or an empty array.</param>
    /// <param name="signerProtected">The exact encoded protected signer header map, or an empty array.</param>
    /// <param name="payload">The complete attached or detached payload bytes.</param>
    /// <returns>The encoded Sig_structure, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateSignature(
        byte[] bodyProtected,
        byte[] signerProtected,
        byte[] payload)
    {
        return CreateSignature(
            bodyProtected,
            signerProtected,
            Array.Empty<byte>(),
            payload,
            new TokenPolicy());
    }

    /// <summary>Constructs a COSE_Sign Sig_structure with external authenticated data.</summary>
    /// <param name="bodyProtected">The exact encoded protected body header map, or an empty array.</param>
    /// <param name="signerProtected">The exact encoded protected signer header map, or an empty array.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes, or an empty array.</param>
    /// <param name="payload">The complete attached or detached payload bytes.</param>
    /// <returns>The encoded Sig_structure, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateSignature(
        byte[] bodyProtected,
        byte[] signerProtected,
        byte[] externalAuthenticatedData,
        byte[] payload)
    {
        return CreateSignature(
            bodyProtected,
            signerProtected,
            externalAuthenticatedData,
            payload,
            new TokenPolicy());
    }

    /// <summary>Constructs a size-limited COSE_Sign Sig_structure.</summary>
    /// <param name="bodyProtected">The exact encoded protected body header map, or an empty array.</param>
    /// <param name="signerProtected">The exact encoded protected signer header map, or an empty array.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes, or an empty array.</param>
    /// <param name="payload">The complete attached or detached payload bytes.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded Sig_structure, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateSignature(
        byte[] bodyProtected,
        byte[] signerProtected,
        byte[] externalAuthenticatedData,
        byte[] payload,
        TokenPolicy policy)
    {
        if (bodyProtected == null)
        {
            throw new ArgumentNullException("bodyProtected");
        }

        if (signerProtected == null)
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

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        return Create(
            "Signature",
            bodyProtected,
            signerProtected,
            externalAuthenticatedData,
            payload,
            policy);
    }

    static TokenBytesResult Create(
        string context,
        byte[] bodyProtected,
        byte[]? signerProtected,
        byte[] externalAuthenticatedData,
        byte[] payload,
        TokenPolicy policy)
    {
        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumCborDepth <= 0)
        {
            return Failure(
                "invalid_policy",
                "The COSE Sig_structure policy contains an invalid size or depth limit.");
        }

        int length = signerProtected == null ? 4 : 5;
        CborItem[] fields = new CborItem[length];
        fields[0] = TextString(context);
        fields[1] = ByteString(bodyProtected);
        int index = 2;
        if (signerProtected != null)
        {
            fields[index] = ByteString(signerProtected);
            index += 1;
        }

        fields[index] = ByteString(externalAuthenticatedData);
        fields[index + 1] = ByteString(payload);

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
