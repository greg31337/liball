using System;
namespace token;

/// <summary>Represents and serializes an RFC 9052 COSE_Encrypt0 message.</summary>
public class CoseEncrypt0
{
    const ulong Encrypt0Tag = 16;

    /// <summary>Contains the protected and unprotected content headers.</summary>
    public CoseHeader? Header;

    /// <summary>Contains the attached ciphertext, or <c>null</c> for detached ciphertext.</summary>
    public byte[]? Ciphertext;

    /// <summary>Indicates whether the ciphertext is detached and encoded as CBOR null.</summary>
    public bool DetachedCiphertext;

    /// <summary>Indicates whether the registered COSE_Encrypt0 tag is written or was parsed.</summary>
    public bool Tagged;

    /// <summary>Contains plaintext after successful authenticated decryption.</summary>
    public byte[]? Plaintext;

    /// <summary>Indicates whether <see cref="Plaintext"/> is available.</summary>
    public bool PlaintextAvailable;

    /// <summary>Indicates whether authenticated content decryption succeeded.</summary>
    public bool AuthenticationTagVerified;

    /// <summary>Indicates whether authenticated or explicitly diagnostic ciphertext bytes are available.</summary>
    public bool CiphertextAvailable;

    /// <summary>Initializes an empty tagged message model.</summary>
    public CoseEncrypt0()
    {
        Header = new CoseHeader();
        Ciphertext = Array.Empty<byte>();
        DetachedCiphertext = false;
        Tagged = true;
        Plaintext = null;
        PlaintextAvailable = false;
        AuthenticationTagVerified = false;
        CiphertextAvailable = false;
    }

    /// <summary>Parses a tagged or contextually untagged COSE_Encrypt0 message using default limits.</summary>
    /// <param name="cbor">The encoded COSE_Encrypt0 bytes.</param>
    /// <returns>The parsed message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>cbor</c> is null.</exception>
    internal static CoseEncrypt0Result Read(byte[] cbor)
    {
        return Read(cbor, new TokenPolicy());
    }

    /// <summary>Parses a tagged or contextually untagged COSE_Encrypt0 message using explicit limits.</summary>
    /// <param name="cbor">The encoded COSE_Encrypt0 bytes.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>The parsed message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseEncrypt0Result Read(byte[] cbor, TokenPolicy policy)
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
            CoseMessageKind.Encrypt0,
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

        CoseEncrypt0 message = new CoseEncrypt0();
        message.Header = header.Header;
        message.DetachedCiphertext =
            fields[2].Kind == TokenCborValueKind.Null;
        message.Ciphertext = message.DetachedCiphertext
            ? null
            : Copy(fields[2].Bytes);
        message.Tagged = decoded.Tagged;

        CoseEncrypt0Result result = new CoseEncrypt0Result();
        result.Success = true;
        result.Message = message;
        return result;
    }

    /// <summary>Writes this message using its <see cref="Tagged"/> setting and default limits.</summary>
    /// <returns>Deterministic COSE_Encrypt0 bytes or stable failure details.</returns>
    internal TokenBytesResult Write()
    {
        return Write(this, Tagged, new TokenPolicy());
    }

    /// <summary>Writes this message using its <see cref="Tagged"/> setting and explicit limits.</summary>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>Deterministic COSE_Encrypt0 bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>policy</c> is null.</exception>
    internal TokenBytesResult WriteWithPolicy(TokenPolicy policy)
    {
        return Write(this, Tagged, policy);
    }

    /// <summary>Writes this message with explicit registered-tag control and default limits.</summary>
    /// <param name="tagged">Whether to include CBOR tag 16.</param>
    /// <returns>Deterministic COSE_Encrypt0 bytes or stable failure details.</returns>
    internal TokenBytesResult WriteWithTag(bool tagged)
    {
        return Write(this, tagged, new TokenPolicy());
    }

    /// <summary>Writes this message with explicit registered-tag control and limits.</summary>
    /// <param name="tagged">Whether to include CBOR tag 16.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>Deterministic COSE_Encrypt0 bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>policy</c> is null.</exception>
    internal TokenBytesResult WriteWithTagAndPolicy(bool tagged, TokenPolicy policy)
    {
        return Write(this, tagged, policy);
    }

    /// <summary>Writes a message using its <see cref="Tagged"/> setting and default limits.</summary>
    /// <param name="message">The message to write.</param>
    /// <returns>Deterministic COSE_Encrypt0 bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>message</c> is null.</exception>
    internal static TokenBytesResult Write(CoseEncrypt0 message)
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
    /// <returns>Deterministic COSE_Encrypt0 bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Write(
        CoseEncrypt0 message,
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
    /// <param name="tagged">Whether to include CBOR tag 16.</param>
    /// <returns>Deterministic COSE_Encrypt0 bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>message</c> is null.</exception>
    internal static TokenBytesResult Write(CoseEncrypt0 message, bool tagged)
    {
        return Write(message, tagged, new TokenPolicy());
    }

    /// <summary>Writes a message with explicit registered-tag control and limits.</summary>
    /// <param name="message">The message to write.</param>
    /// <param name="tagged">Whether to include CBOR tag 16.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>Deterministic COSE_Encrypt0 bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Write(
        CoseEncrypt0 message,
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

        CborItem ciphertext = message.DetachedCiphertext
            ? NullItem()
            : BytesItem(message.Ciphertext);
        CborItem structure = ArrayItem(new CborItem[]
        {
            BytesItem(protectedBytes.Bytes),
            MapItem((message.Header ?? throw new InvalidOperationException("A required value is null.")).UnprotectedParameters),
            ciphertext
        });
        CborItem output = tagged
            ? TagItem(Encrypt0Tag, structure)
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

    internal static TokenResult ValidateModel(CoseEncrypt0 message)
    {
        if (message.Header == null)
        {
            return ValidationFailure(
                "invalid_cose_encrypt0_header",
                "A COSE_Encrypt0 message requires headers.");
        }

        if (!message.DetachedCiphertext && message.Ciphertext == null)
        {
            return ValidationFailure(
                "invalid_cose_encrypt0_ciphertext",
                "An attached COSE_Encrypt0 message requires ciphertext bytes.");
        }

        if (message.DetachedCiphertext && message.Ciphertext != null)
        {
            return ValidationFailure(
                "invalid_cose_encrypt0_ciphertext",
                "A detached COSE_Encrypt0 message must not contain attached ciphertext bytes.");
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
                "The COSE_Encrypt0 policy contains an invalid size or depth limit.");
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

    internal static CoseEncrypt0Result Failure(TokenError? error)
    {
        CoseEncrypt0Result result = new CoseEncrypt0Result();
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
