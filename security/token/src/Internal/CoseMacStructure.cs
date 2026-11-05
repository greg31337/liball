using System;
namespace token;

/// <summary>Constructs deterministic RFC 9052 MAC_structure values.</summary>
internal static class CoseMacStructure
{
    /// <summary>Constructs a COSE_Mac MAC_structure with empty external authenticated data.</summary>
    /// <param name="protectedParameters">The exact encoded protected header map, or an empty array.</param>
    /// <param name="payload">The complete attached or detached payload bytes.</param>
    /// <returns>The encoded MAC_structure, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateMac(
        byte[] protectedParameters,
        byte[] payload)
    {
        return CreateMac(
            protectedParameters,
            Array.Empty<byte>(),
            payload,
            new TokenPolicy());
    }

    /// <summary>Constructs a COSE_Mac MAC_structure with external authenticated data.</summary>
    /// <param name="protectedParameters">The exact encoded protected header map, or an empty array.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes, or an empty array.</param>
    /// <param name="payload">The complete attached or detached payload bytes.</param>
    /// <returns>The encoded MAC_structure, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateMac(
        byte[] protectedParameters,
        byte[] externalAuthenticatedData,
        byte[] payload)
    {
        return CreateMac(
            protectedParameters,
            externalAuthenticatedData,
            payload,
            new TokenPolicy());
    }

    /// <summary>Constructs a size-limited COSE_Mac MAC_structure.</summary>
    /// <param name="protectedParameters">The exact encoded protected header map, or an empty array.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes, or an empty array.</param>
    /// <param name="payload">The complete attached or detached payload bytes.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded MAC_structure, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateMac(
        byte[] protectedParameters,
        byte[] externalAuthenticatedData,
        byte[] payload,
        TokenPolicy policy)
    {
        return Create(
            "MAC",
            protectedParameters,
            externalAuthenticatedData,
            payload,
            policy);
    }

    /// <summary>Constructs a COSE_Mac0 MAC_structure with empty external authenticated data.</summary>
    /// <param name="protectedParameters">The exact encoded protected header map, or an empty array.</param>
    /// <param name="payload">The complete attached or detached payload bytes.</param>
    /// <returns>The encoded MAC_structure, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateMac0(
        byte[] protectedParameters,
        byte[] payload)
    {
        return CreateMac0(
            protectedParameters,
            Array.Empty<byte>(),
            payload,
            new TokenPolicy());
    }

    /// <summary>Constructs a COSE_Mac0 MAC_structure with external authenticated data.</summary>
    /// <param name="protectedParameters">The exact encoded protected header map, or an empty array.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes, or an empty array.</param>
    /// <param name="payload">The complete attached or detached payload bytes.</param>
    /// <returns>The encoded MAC_structure, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateMac0(
        byte[] protectedParameters,
        byte[] externalAuthenticatedData,
        byte[] payload)
    {
        return CreateMac0(
            protectedParameters,
            externalAuthenticatedData,
            payload,
            new TokenPolicy());
    }

    /// <summary>Constructs a size-limited COSE_Mac0 MAC_structure.</summary>
    /// <param name="protectedParameters">The exact encoded protected header map, or an empty array.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes, or an empty array.</param>
    /// <param name="payload">The complete attached or detached payload bytes.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded MAC_structure, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult CreateMac0(
        byte[] protectedParameters,
        byte[] externalAuthenticatedData,
        byte[] payload,
        TokenPolicy policy)
    {
        return Create(
            "MAC0",
            protectedParameters,
            externalAuthenticatedData,
            payload,
            policy);
    }

    static TokenBytesResult Create(
        string context,
        byte[] protectedParameters,
        byte[] externalAuthenticatedData,
        byte[] payload,
        TokenPolicy policy)
    {
        if (protectedParameters == null)
        {
            throw new ArgumentNullException("protectedParameters");
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

        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumCborDepth <= 0)
        {
            return Failure(
                "invalid_policy",
                "The COSE MAC_structure policy contains an invalid size or depth limit.");
        }

        CborItem[] fields = new CborItem[4];
        fields[0] = TextString(context);
        fields[1] = ByteString(protectedParameters);
        fields[2] = ByteString(externalAuthenticatedData);
        fields[3] = ByteString(payload);

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
