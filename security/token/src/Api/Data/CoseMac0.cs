using System;
namespace token;

/// <summary>Represents and serializes an RFC 9052 COSE_Mac0 message.</summary>
public class CoseMac0
{
    const ulong Mac0Tag = 17;

    /// <summary>Contains the protected and unprotected MAC headers.</summary>
    public CoseHeader? Header;

    /// <summary>Contains the attached payload, or <c>null</c> for a detached payload.</summary>
    public byte[]? Payload;

    /// <summary>Indicates whether the payload is detached and encoded as CBOR null.</summary>
    public bool DetachedPayload;

    /// <summary>Contains the message authentication code bytes.</summary>
    public byte[]? Tag;

    /// <summary>Indicates whether the registered COSE_Mac0 tag is written or was parsed.</summary>
    public bool Tagged;

    /// <summary>Indicates whether the message authentication code has been verified.</summary>
    public bool TagVerified;

    /// <summary>Indicates whether authenticated or explicitly diagnostic payload bytes are available.</summary>
    public bool PayloadAvailable;

    /// <summary>Initializes an empty tagged message model.</summary>
    public CoseMac0()
    {
        Header = new CoseHeader();
        Payload = Array.Empty<byte>();
        DetachedPayload = false;
        Tag = Array.Empty<byte>();
        Tagged = true;
        TagVerified = false;
        PayloadAvailable = false;
    }

    /// <summary>Parses a tagged or contextually untagged COSE_Mac0 message using default limits.</summary>
    /// <param name="cbor">The encoded COSE_Mac0 bytes.</param>
    /// <returns>The parsed message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>cbor</c> is null.</exception>
    internal static CoseMac0Result Read(byte[] cbor)
    {
        return Read(cbor, new TokenPolicy());
    }

    /// <summary>Parses a tagged or contextually untagged COSE_Mac0 message using explicit limits.</summary>
    /// <param name="cbor">The encoded COSE_Mac0 bytes.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>The parsed message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseMac0Result Read(byte[] cbor, TokenPolicy policy)
    {
        if (cbor == null)
        {
            throw new ArgumentNullException("cbor");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        CoseMessageResult decoded = CoseMessage.Read(
            cbor,
            CoseMessageKind.Mac0,
            policy);
        if (!decoded.Success)
        {
            return Failure(decoded.Error);
        }

        CborItem[]? fields = (decoded.Item ?? throw new InvalidOperationException("A required value is null.")).ArrayValue;
        CoseHeaderResult header = CoseHeader.FromBuckets(
            (fields ?? throw new InvalidOperationException("A required value is null."))[0],
            fields[1],
            policy);
        if (!header.Success)
        {
            return Failure(header.Error);
        }

        CoseMac0 message = new CoseMac0();
        message.Header = header.Header;
        message.DetachedPayload =
            fields[2].Kind == TokenCborValueKind.Null;
        message.Payload = message.DetachedPayload
            ? null
            : Copy(fields[2].Bytes);
        message.Tag = Copy(fields[3].Bytes);
        message.Tagged = decoded.Tagged;

        CoseMac0Result result = new CoseMac0Result();
        result.Success = true;
        result.Message = message;
        return result;
    }

    /// <summary>Writes this message using its <see cref="Tagged"/> setting and default limits.</summary>
    /// <returns>Deterministic COSE_Mac0 bytes or stable failure details.</returns>
    internal TokenBytesResult Write()
    {
        return Write(this, Tagged, new TokenPolicy());
    }

    /// <summary>Writes this message using its <see cref="Tagged"/> setting and explicit limits.</summary>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>Deterministic COSE_Mac0 bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>policy</c> is null.</exception>
    internal TokenBytesResult WriteWithPolicy(TokenPolicy policy)
    {
        return Write(this, Tagged, policy);
    }

    /// <summary>Writes this message with explicit registered-tag control and default limits.</summary>
    /// <param name="tagged">Whether to include CBOR tag 17.</param>
    /// <returns>Deterministic COSE_Mac0 bytes or stable failure details.</returns>
    internal TokenBytesResult WriteWithTag(bool tagged)
    {
        return Write(this, tagged, new TokenPolicy());
    }

    /// <summary>Writes this message with explicit registered-tag control and limits.</summary>
    /// <param name="tagged">Whether to include CBOR tag 17.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>Deterministic COSE_Mac0 bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>policy</c> is null.</exception>
    internal TokenBytesResult WriteWithTagAndPolicy(bool tagged, TokenPolicy policy)
    {
        return Write(this, tagged, policy);
    }

    /// <summary>Writes a message using its <see cref="Tagged"/> setting and default limits.</summary>
    /// <param name="message">The message to write.</param>
    /// <returns>Deterministic COSE_Mac0 bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>message</c> is null.</exception>
    internal static TokenBytesResult Write(CoseMac0 message)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        return Write(message, message.Tagged, new TokenPolicy());
    }

    /// <summary>Writes a message using its <see cref="Tagged"/> setting and explicit limits.</summary>
    /// <param name="message">The message to write.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>Deterministic COSE_Mac0 bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Write(
        CoseMac0 message,
        TokenPolicy policy)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        return Write(message, message.Tagged, policy);
    }

    /// <summary>Writes a message with explicit registered-tag control and default limits.</summary>
    /// <param name="message">The message to write.</param>
    /// <param name="tagged">Whether to include CBOR tag 17.</param>
    /// <returns>Deterministic COSE_Mac0 bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>message</c> is null.</exception>
    internal static TokenBytesResult Write(CoseMac0 message, bool tagged)
    {
        return Write(message, tagged, new TokenPolicy());
    }

    /// <summary>Writes a message with explicit registered-tag control and limits.</summary>
    /// <param name="message">The message to write.</param>
    /// <param name="tagged">Whether to include CBOR tag 17.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>Deterministic COSE_Mac0 bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Write(
        CoseMac0 message,
        bool tagged,
        TokenPolicy policy)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
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

        TokenResult modelResult = ValidateModel(message);
        if (!modelResult.Success)
        {
            return BytesFailure(modelResult.Error);
        }

        TokenBytesResult protectedBytes = CoseSign.GetProtectedBytes(
            message.Header,
            policy);
        if (!protectedBytes.Success)
        {
            return protectedBytes;
        }

        CborItem payload = message.DetachedPayload
            ? NullItem()
            : BytesItem(message.Payload);
        CborItem structure = ArrayItem(new CborItem[]
        {
            BytesItem(protectedBytes.Bytes),
            MapItem((message.Header ?? throw new InvalidOperationException("A required value is null.")).UnprotectedParameters),
            payload,
            BytesItem(message.Tag)
        });
        CborItem output = tagged
            ? TagItem(Mac0Tag, structure)
            : structure;
        CborWriter writer = CborWriter.Create(
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        TokenResult written = writer.WriteDeterministicItem(output);
        if (!written.Success)
        {
            return BytesFailure(written.Error);
        }

        message.Header.ProtectedBytes = Copy(protectedBytes.Bytes);
        message.Tagged = tagged;
        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = writer.ToArray();
        return result;
    }

    internal static TokenResult ValidateModel(CoseMac0 message)
    {
        if (message.Header == null)
        {
            return ValidationFailure(
                "invalid_cose_mac0_header",
                "A COSE_Mac0 message requires headers.");
        }

        if (!message.DetachedPayload && message.Payload == null)
        {
            return ValidationFailure(
                "invalid_cose_mac0_payload",
                "An attached COSE_Mac0 message requires payload bytes.");
        }

        if (message.DetachedPayload && message.Payload != null)
        {
            return ValidationFailure(
                "invalid_cose_mac0_payload",
                "A detached COSE_Mac0 message must not contain attached payload bytes.");
        }

        if (message.Tag == null)
        {
            return ValidationFailure(
                "invalid_cose_mac0_tag",
                "A COSE_Mac0 message requires authentication tag bytes.");
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
                "The COSE_Mac0 policy contains an invalid size or depth limit.");
        }

        return ValidationSuccess();
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

    internal static CborItem NullItem()
    {
        return new CborItem();
    }

    internal static CborItem TagItem(ulong tag, CborItem value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Tag;
        item.TagValue = tag;
        item.TaggedValue = value;
        return item;
    }

    internal static byte[] Copy(byte[]? value)
    {
        return value.AsSpan().ToArray();
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

    internal static CoseMac0Result Failure(TokenError? error)
    {
        CoseMac0Result result = new CoseMac0Result();
        result.Error = error;
        return result;
    }

    internal static TokenBytesResult BytesFailure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }
}
