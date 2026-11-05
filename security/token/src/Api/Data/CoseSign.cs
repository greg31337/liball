using System;
namespace token;

/// <summary>Represents and serializes an RFC 9052 COSE_Sign message.</summary>
public class CoseSign
{
    const ulong SignTag = 98;

    /// <summary>Contains the message protected and unprotected headers.</summary>
    public CoseHeader? Header;

    /// <summary>Contains the attached payload, or <c>null</c> for a detached payload.</summary>
    public byte[]? Payload;

    /// <summary>Indicates whether the payload is detached and encoded as CBOR null.</summary>
    public bool DetachedPayload;

    /// <summary>Contains one or more signatures in serialization order.</summary>
    public CoseSignature[] Signatures;

    /// <summary>Indicates whether the registered COSE_Sign tag is written or was parsed.</summary>
    public bool Tagged;

    /// <summary>Indicates whether every signature has been cryptographically verified.</summary>
    public bool SignaturesVerified;

    /// <summary>Indicates whether authenticated or explicitly diagnostic payload bytes are available.</summary>
    public bool PayloadAvailable;

    /// <summary>Initializes an empty tagged message model.</summary>
    public CoseSign()
    {
        Header = new CoseHeader();
        Payload = Array.Empty<byte>();
        DetachedPayload = false;
        Signatures = Array.Empty<CoseSignature>();
        Tagged = true;
        SignaturesVerified = false;
        PayloadAvailable = false;
    }

    /// <summary>Parses a tagged or contextually untagged COSE_Sign message using default limits.</summary>
    /// <param name="cbor">The encoded COSE_Sign bytes.</param>
    /// <returns>The parsed message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>cbor</c> is null.</exception>
    internal static CoseSignResult Read(byte[] cbor)
    {
        return Read(cbor, new TokenPolicy());
    }

    /// <summary>Parses a tagged or contextually untagged COSE_Sign message using explicit limits.</summary>
    /// <param name="cbor">The encoded COSE_Sign bytes.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>The parsed message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseSignResult Read(byte[] cbor, TokenPolicy policy)
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
            CoseMessageKind.Sign,
            policy);
        if (!decoded.Success)
        {
            return Failure(decoded.Error);
        }

        CborItem[]? fields = (decoded.Item ?? throw new InvalidOperationException("A required value is null.")).ArrayValue;
        CoseHeaderResult bodyHeader = CoseHeader.FromBuckets(
            (fields ?? throw new InvalidOperationException("A required value is null."))[0],
            fields[1],
            policy);
        if (!bodyHeader.Success)
        {
            return Failure(bodyHeader.Error);
        }

        CborItem[]? signatureItems = fields[3].ArrayValue;
        CoseSignature[] signatures =
            new CoseSignature[(signatureItems ?? throw new InvalidOperationException("A required value is null.")).Length];
        int index = 0;
        while (index < signatureItems.Length)
        {
            CoseSignatureResult signature = ReadSignature(
                signatureItems[index],
                policy,
                index);
            if (!signature.Success)
            {
                return Failure(signature.Error);
            }

            signatures[index] = (signature.Signature ?? throw new InvalidOperationException("A required value is null."));
            index += 1;
        }

        CoseSign message = new CoseSign();
        message.Header = bodyHeader.Header;
        message.DetachedPayload =
            fields[2].Kind == TokenCborValueKind.Null;
        message.Payload = message.DetachedPayload
            ? null
            : Copy(fields[2].Bytes);
        message.Signatures = signatures;
        message.Tagged = decoded.Tagged;

        CoseSignResult result = new CoseSignResult();
        result.Success = true;
        result.Message = message;
        return result;
    }

    /// <summary>Writes this message using its <see cref="Tagged"/> setting and default limits.</summary>
    /// <returns>Deterministic COSE_Sign bytes or stable failure details.</returns>
    internal TokenBytesResult Write()
    {
        return Write(this, Tagged, new TokenPolicy());
    }

    /// <summary>Writes this message using its <see cref="Tagged"/> setting and explicit limits.</summary>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>Deterministic COSE_Sign bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>policy</c> is null.</exception>
    internal TokenBytesResult WriteWithPolicy(TokenPolicy policy)
    {
        return Write(this, Tagged, policy);
    }

    /// <summary>Writes this message with explicit registered-tag control and default limits.</summary>
    /// <param name="tagged">Whether to include CBOR tag 98.</param>
    /// <returns>Deterministic COSE_Sign bytes or stable failure details.</returns>
    internal TokenBytesResult WriteWithTag(bool tagged)
    {
        return Write(this, tagged, new TokenPolicy());
    }

    /// <summary>Writes this message with explicit registered-tag control and limits.</summary>
    /// <param name="tagged">Whether to include CBOR tag 98.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>Deterministic COSE_Sign bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>policy</c> is null.</exception>
    internal TokenBytesResult WriteWithTagAndPolicy(bool tagged, TokenPolicy policy)
    {
        return Write(this, tagged, policy);
    }

    /// <summary>Writes a message using its <see cref="Tagged"/> setting and default limits.</summary>
    /// <param name="message">The message to write.</param>
    /// <returns>Deterministic COSE_Sign bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>message</c> is null.</exception>
    internal static TokenBytesResult Write(CoseSign message)
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
    /// <returns>Deterministic COSE_Sign bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Write(
        CoseSign message,
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
    /// <param name="tagged">Whether to include CBOR tag 98.</param>
    /// <returns>Deterministic COSE_Sign bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>message</c> is null.</exception>
    internal static TokenBytesResult Write(CoseSign message, bool tagged)
    {
        return Write(message, tagged, new TokenPolicy());
    }

    /// <summary>Writes a message with explicit registered-tag control and limits.</summary>
    /// <param name="message">The message to write.</param>
    /// <param name="tagged">Whether to include CBOR tag 98.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>Deterministic COSE_Sign bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Write(
        CoseSign message,
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

        TokenBytesResult bodyProtected = GetProtectedBytes(
            message.Header,
            policy);
        if (!bodyProtected.Success)
        {
            return bodyProtected;
        }

        CborItem[] signatureItems =
            new CborItem[message.Signatures.Length];
        byte[][] protectedBytes =
            new byte[message.Signatures.Length][];
        int index = 0;
        while (index < message.Signatures.Length)
        {
            CoseSignature signature = message.Signatures[index];
            TokenBytesResult signerProtected = GetProtectedBytes(
                signature.Header,
                policy);
            if (!signerProtected.Success)
            {
                return signerProtected;
            }

            protectedBytes[index] = (signerProtected.Bytes ?? throw new InvalidOperationException("A required value is null."));
            signatureItems[index] = ArrayItem(new CborItem[]
            {
                BytesItem(signerProtected.Bytes),
                MapItem((signature.Header ?? throw new InvalidOperationException("A required value is null.")).UnprotectedParameters),
                BytesItem(signature.Signature)
            });
            index += 1;
        }

        CborItem payload = message.DetachedPayload
            ? NullItem()
            : BytesItem(message.Payload);
        CborItem structure = ArrayItem(new CborItem[]
        {
            BytesItem(bodyProtected.Bytes),
            MapItem((message.Header ?? throw new InvalidOperationException("A required value is null.")).UnprotectedParameters),
            payload,
            ArrayItem(signatureItems)
        });
        CborItem output = tagged ? TagItem(SignTag, structure) : structure;
        CborWriter writer = CborWriter.Create(
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        TokenResult written = writer.WriteDeterministicItem(output);
        if (!written.Success)
        {
            return BytesFailure(written.Error);
        }

        message.Header.ProtectedBytes = Copy(bodyProtected.Bytes);
        index = 0;
        while (index < message.Signatures.Length)
        {
            (message.Signatures[index].Header ?? throw new InvalidOperationException("A required value is null.")).ProtectedBytes =
                Copy(protectedBytes[index]);
            index += 1;
        }

        message.Tagged = tagged;
        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = writer.ToArray();
        return result;
    }

    internal static CoseSignatureResult ReadSignature(
        CborItem item,
        TokenPolicy policy,
        int position)
    {
        if (item == null
            || item.Kind != TokenCborValueKind.Array
            || item.ArrayValue == null
            || item.ArrayValue.Length != 3)
        {
            return SignatureFailure(
                "invalid_cose_signature_structure",
                "A COSE_Signature must be a three-item CBOR array.",
                position);
        }

        CborItem protectedBucket = item.ArrayValue[0];
        CborItem unprotectedBucket = item.ArrayValue[1];
        CborItem signatureItem = item.ArrayValue[2];
        if (signatureItem == null
            || signatureItem.Kind != TokenCborValueKind.ByteString
            || signatureItem.Bytes == null)
        {
            return SignatureFailure(
                "invalid_cose_signature_structure",
                "The COSE_Signature value must be a byte string.",
                position);
        }

        if (protectedBucket == null || unprotectedBucket == null)
        {
            return SignatureFailure(
                "invalid_cose_signature_structure",
                "The COSE_Signature header buckets are missing.",
                position);
        }

        CoseHeaderResult header = CoseHeader.FromBuckets(
            protectedBucket,
            unprotectedBucket,
            policy);
        if (!header.Success)
        {
            return SignatureFailure(header.Error);
        }

        CoseSignature signature = new CoseSignature();
        signature.Header = header.Header;
        signature.Signature = Copy(signatureItem.Bytes);
        CoseSignatureResult result = new CoseSignatureResult();
        result.Success = true;
        result.Signature = signature;
        return result;
    }

    internal static TokenResult ValidateModel(CoseSign message)
    {
        if (message.Header == null)
        {
            return ValidationFailure(
                "invalid_cose_sign_header",
                "A COSE_Sign message requires body headers.");
        }

        if (!message.DetachedPayload && message.Payload == null)
        {
            return ValidationFailure(
                "invalid_cose_sign_payload",
                "An attached COSE_Sign message requires payload bytes.");
        }

        if (message.DetachedPayload && message.Payload != null)
        {
            return ValidationFailure(
                "invalid_cose_sign_payload",
                "A detached COSE_Sign message must not contain attached payload bytes.");
        }

        if (message.Signatures == null
            || message.Signatures.Length == 0)
        {
            return ValidationFailure(
                "invalid_cose_sign_signatures",
                "A COSE_Sign message requires at least one signature.");
        }

        int index = 0;
        while (index < message.Signatures.Length)
        {
            CoseSignature signature = message.Signatures[index];
            if (signature == null
                || signature.Header == null
                || signature.Signature == null)
            {
                return ValidationFailure(
                    "invalid_cose_sign_signature",
                    "Each COSE_Signature requires headers and signature bytes.");
            }

            index += 1;
        }

        return ValidationSuccess();
    }

    internal static TokenBytesResult GetProtectedBytes(
        CoseHeader? header,
        TokenPolicy policy)
    {
        TokenBytesResult encoded = CoseHeader.WriteProtected(
            (header ?? throw new InvalidOperationException("A required value is null.")),
            policy);
        if (!encoded.Success
            || header.ProtectedBytes == null
            || header.ProtectedBytes.Length == 0)
        {
            return encoded;
        }

        CborReader reader = CborReader.Create(
            header.ProtectedBytes,
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        CborReadResult parsed = reader.Read();
        CborItem parameters = MapItem(header.ProtectedParameters);
        if (parsed.Success
            && reader.GetRemainingLength() == 0
            && (parsed.Item ?? throw new InvalidOperationException("A required value is null.")).Kind == TokenCborValueKind.Map
            && CborMapKeyComparer.AreEqual(
                parsed.Item,
                parameters,
                policy.MaximumCborDepth))
        {
            TokenBytesResult preserved = new TokenBytesResult();
            preserved.Success = true;
            preserved.Bytes = Copy(header.ProtectedBytes);
            return preserved;
        }

        return encoded;
    }

    internal static TokenResult ValidatePolicy(TokenPolicy policy)
    {
        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumCborDepth <= 0)
        {
            return ValidationFailure(
                "invalid_policy",
                "The COSE_Sign policy contains an invalid size or depth limit.");
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

    internal static CoseSignResult Failure(TokenError? error)
    {
        CoseSignResult result = new CoseSignResult();
        result.Error = error;
        return result;
    }

    internal static TokenBytesResult BytesFailure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }

    internal static CoseSignatureResult SignatureFailure(TokenError? error)
    {
        CoseSignatureResult result = new CoseSignatureResult();
        result.Error = error;
        return result;
    }

    internal static CoseSignatureResult SignatureFailure(
        string code,
        string message,
        int position)
    {
        return SignatureFailure(new TokenError(code, message, position));
    }
}
