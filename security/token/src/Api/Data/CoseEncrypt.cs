using System;
using System.Collections.Generic;

namespace token;

/// <summary>Represents and serializes an RFC 9052 COSE_Encrypt message.</summary>
public class CoseEncrypt
{
    const ulong EncryptTag = 96;

    /// <summary>Contains the protected and unprotected content headers.</summary>
    public CoseHeader? Header;

    /// <summary>Contains the attached ciphertext, or <c>null</c> for detached ciphertext.</summary>
    public byte[]? Ciphertext;

    /// <summary>Indicates whether the ciphertext is detached and encoded as CBOR null.</summary>
    public bool DetachedCiphertext;

    /// <summary>Contains one or more recipient information structures.</summary>
    public CoseRecipient[] Recipients;

    /// <summary>Indicates whether the registered COSE_Encrypt tag is written or was parsed.</summary>
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
    public CoseEncrypt()
    {
        Header = new CoseHeader();
        Ciphertext = Array.Empty<byte>();
        DetachedCiphertext = false;
        Recipients = Array.Empty<CoseRecipient>();
        Tagged = true;
        Plaintext = null;
        PlaintextAvailable = false;
        AuthenticationTagVerified = false;
        CiphertextAvailable = false;
    }

    /// <summary>Parses a tagged or contextually untagged COSE_Encrypt message using default limits.</summary>
    /// <param name="cbor">The encoded COSE_Encrypt bytes.</param>
    /// <returns>The parsed message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>cbor</c> is null.</exception>
    internal static CoseEncryptResult Read(byte[] cbor)
    {
        return Read(cbor, new TokenPolicy());
    }

    /// <summary>Parses a tagged or contextually untagged COSE_Encrypt message using explicit limits.</summary>
    /// <param name="cbor">The encoded COSE_Encrypt bytes.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>The parsed message or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseEncryptResult Read(byte[] cbor, TokenPolicy policy)
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
            CoseMessageKind.Encrypt,
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

        CborItem[]? recipientItems = fields[3].ArrayValue;
        CoseRecipient[] recipients =
            new CoseRecipient[(recipientItems ?? throw new InvalidOperationException("A required value is null.")).Length];
        int index = 0;
        while (index < recipientItems.Length)
        {
            CoseRecipientResult recipient = ReadRecipient(
                recipientItems[index],
                policy,
                index);
            if (!recipient.Success)
            {
                return Failure(recipient.Error);
            }

            recipients[index] = (recipient.Recipient ?? throw new InvalidOperationException("A required value is null."));
            index += 1;
        }

        CoseEncrypt message = new CoseEncrypt();
        message.Header = header.Header;
        message.DetachedCiphertext =
            fields[2].Kind == TokenCborValueKind.Null;
        message.Ciphertext = message.DetachedCiphertext
            ? null
            : Copy(fields[2].Bytes);
        message.Recipients = recipients;
        message.Tagged = decoded.Tagged;

        CoseEncryptResult result = new CoseEncryptResult();
        result.Success = true;
        result.Message = message;
        return result;
    }

    /// <summary>Writes this message using its <see cref="Tagged"/> setting and default limits.</summary>
    /// <returns>Deterministic COSE_Encrypt bytes or stable failure details.</returns>
    internal TokenBytesResult Write()
    {
        return Write(this, Tagged, new TokenPolicy());
    }

    /// <summary>Writes this message using its <see cref="Tagged"/> setting and explicit limits.</summary>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>Deterministic COSE_Encrypt bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>policy</c> is null.</exception>
    internal TokenBytesResult WriteWithPolicy(TokenPolicy policy)
    {
        return Write(this, Tagged, policy);
    }

    /// <summary>Writes this message with explicit registered-tag control and default limits.</summary>
    /// <param name="tagged">Whether to include CBOR tag 96.</param>
    /// <returns>Deterministic COSE_Encrypt bytes or stable failure details.</returns>
    internal TokenBytesResult WriteWithTag(bool tagged)
    {
        return Write(this, tagged, new TokenPolicy());
    }

    /// <summary>Writes this message with explicit registered-tag control and limits.</summary>
    /// <param name="tagged">Whether to include CBOR tag 96.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>Deterministic COSE_Encrypt bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>policy</c> is null.</exception>
    internal TokenBytesResult WriteWithTagAndPolicy(bool tagged, TokenPolicy policy)
    {
        return Write(this, tagged, policy);
    }

    /// <summary>Writes a message using its <see cref="Tagged"/> setting and default limits.</summary>
    /// <param name="message">The message to write.</param>
    /// <returns>Deterministic COSE_Encrypt bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>message</c> is null.</exception>
    internal static TokenBytesResult Write(CoseEncrypt message)
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
    /// <returns>Deterministic COSE_Encrypt bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Write(
        CoseEncrypt message,
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
    /// <param name="tagged">Whether to include CBOR tag 96.</param>
    /// <returns>Deterministic COSE_Encrypt bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>message</c> is null.</exception>
    internal static TokenBytesResult Write(CoseEncrypt message, bool tagged)
    {
        return Write(message, tagged, new TokenPolicy());
    }

    /// <summary>Writes a message with explicit registered-tag control and limits.</summary>
    /// <param name="message">The message to write.</param>
    /// <param name="tagged">Whether to include CBOR tag 96.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>Deterministic COSE_Encrypt bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Write(
        CoseEncrypt message,
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

        CborItem[] recipientItems =
            new CborItem[message.Recipients.Length];
        CoseRecipientEncoding[] recipientEncodings =
            new CoseRecipientEncoding[message.Recipients.Length];
        int index = 0;
        while (index < message.Recipients.Length)
        {
            CoseRecipientEncoding encoded = EncodeRecipient(
                message.Recipients[index],
                policy);
            if (!encoded.Success)
            {
                return BytesFailure(encoded.Error);
            }

            recipientEncodings[index] = encoded;
            recipientItems[index] = (encoded.Item ?? throw new InvalidOperationException("A required value is null."));
            index += 1;
        }

        CborItem ciphertext = message.DetachedCiphertext
            ? NullItem()
            : BytesItem(message.Ciphertext);
        CborItem structure = ArrayItem(new CborItem[]
        {
            BytesItem(protectedBytes.Bytes),
            MapItem((message.Header ?? throw new InvalidOperationException("A required value is null.")).UnprotectedParameters),
            ciphertext,
            ArrayItem(recipientItems)
        });
        CborItem output = tagged
            ? TagItem(EncryptTag, structure)
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
        index = 0;
        while (index < message.Recipients.Length)
        {
            CommitRecipient(
                message.Recipients[index],
                recipientEncodings[index]);
            index += 1;
        }

        message.Tagged = tagged;
        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = writer.ToArray();
        return result;
    }

    internal static CoseRecipientResult ReadRecipient(
        CborItem item,
        TokenPolicy policy,
        int position)
    {
        if (item == null
            || item.Kind != TokenCborValueKind.Array
            || item.ArrayValue == null
            || item.ArrayValue.Length < 3
            || item.ArrayValue.Length > 4)
        {
            return RecipientFailure(
                "invalid_cose_recipient_structure",
                "A COSE recipient must be a three- or four-item CBOR array.",
                position);
        }

        CborItem protectedBucket = item.ArrayValue[0];
        CborItem unprotectedBucket = item.ArrayValue[1];
        CborItem encryptedKey = item.ArrayValue[2];
        if (protectedBucket == null || unprotectedBucket == null)
        {
            return RecipientFailure(
                "invalid_cose_recipient_structure",
                "The COSE recipient header buckets are missing.",
                position);
        }

        if (encryptedKey == null
            || encryptedKey.Kind != TokenCborValueKind.ByteString
                && encryptedKey.Kind != TokenCborValueKind.Null
            || encryptedKey.Kind == TokenCborValueKind.ByteString
                && encryptedKey.Bytes == null)
        {
            return RecipientFailure(
                "invalid_cose_recipient_structure",
                "The COSE recipient encrypted key must be a byte string or null.",
                position);
        }

        CoseHeaderResult header = CoseHeader.FromBuckets(
            protectedBucket,
            unprotectedBucket,
            policy);
        if (!header.Success)
        {
            return RecipientFailure(header.Error);
        }

        CoseRecipient[] recipients = Array.Empty<CoseRecipient>();
        if (item.ArrayValue.Length == 4)
        {
            CborItem nested = item.ArrayValue[3];
            if (nested == null
                || nested.Kind != TokenCborValueKind.Array
                || nested.ArrayValue == null
                || nested.ArrayValue.Length == 0)
            {
                return RecipientFailure(
                    "invalid_cose_recipient_structure",
                    "Nested COSE recipients must be a nonempty array.",
                    position);
            }

            recipients = new CoseRecipient[nested.ArrayValue.Length];
            int index = 0;
            while (index < recipients.Length)
            {
                CoseRecipientResult child = ReadRecipient(
                    nested.ArrayValue[index],
                    policy,
                    index);
                if (!child.Success)
                {
                    return child;
                }

                recipients[index] = (child.Recipient ?? throw new InvalidOperationException("A required value is null."));
                index += 1;
            }
        }

        CoseRecipient recipient = new CoseRecipient();
        recipient.Header = header.Header;
        recipient.EncryptedKeyAbsent =
            encryptedKey.Kind == TokenCborValueKind.Null;
        recipient.EncryptedKey = recipient.EncryptedKeyAbsent
            ? null
            : Copy(encryptedKey.Bytes);
        recipient.Recipients = recipients;
        CoseRecipientResult result = new CoseRecipientResult();
        result.Success = true;
        result.Recipient = recipient;
        return result;
    }

    internal static CoseRecipientEncoding EncodeRecipient(
        CoseRecipient recipient,
        TokenPolicy policy)
    {
        TokenBytesResult protectedBytes = CoseSign.GetProtectedBytes(
            recipient.Header,
            policy);
        if (!protectedBytes.Success)
        {
            return RecipientEncodingFailure(protectedBytes.Error);
        }

        CoseRecipientEncoding[] children =
            new CoseRecipientEncoding[recipient.Recipients.Length];
        CborItem[] childItems =
            new CborItem[recipient.Recipients.Length];
        int index = 0;
        while (index < recipient.Recipients.Length)
        {
            CoseRecipientEncoding child = EncodeRecipient(
                recipient.Recipients[index],
                policy);
            if (!child.Success)
            {
                return child;
            }

            children[index] = child;
            childItems[index] = (child.Item ?? throw new InvalidOperationException("A required value is null."));
            index += 1;
        }

        int length = recipient.Recipients.Length == 0 ? 3 : 4;
        CborItem[] fields = new CborItem[length];
        fields[0] = BytesItem(protectedBytes.Bytes);
        fields[1] = MapItem((recipient.Header ?? throw new InvalidOperationException("A required value is null.")).UnprotectedParameters);
        fields[2] = recipient.EncryptedKeyAbsent
            ? NullItem()
            : BytesItem(recipient.EncryptedKey);
        if (length == 4)
        {
            fields[3] = ArrayItem(childItems);
        }

        CoseRecipientEncoding result = new CoseRecipientEncoding();
        result.Success = true;
        result.Item = ArrayItem(fields);
        result.ProtectedBytes = protectedBytes.Bytes;
        result.Children = children;
        return result;
    }

    internal static void CommitRecipient(
        CoseRecipient recipient,
        CoseRecipientEncoding encoding)
    {
        (recipient.Header ?? throw new InvalidOperationException("A required value is null.")).ProtectedBytes = Copy(encoding.ProtectedBytes);
        int index = 0;
        while (index < recipient.Recipients.Length)
        {
            CommitRecipient(
                recipient.Recipients[index],
                (encoding.Children ?? throw new InvalidOperationException("A required value is null."))[index]);
            index += 1;
        }
    }

    internal static TokenResult ValidateModel(CoseEncrypt message)
    {
        if (message.Header == null)
        {
            return ValidationFailure(
                "invalid_cose_encrypt_header",
                "A COSE_Encrypt message requires content headers.");
        }

        if (!message.DetachedCiphertext && message.Ciphertext == null)
        {
            return ValidationFailure(
                "invalid_cose_encrypt_ciphertext",
                "An attached COSE_Encrypt message requires ciphertext bytes.");
        }

        if (message.DetachedCiphertext && message.Ciphertext != null)
        {
            return ValidationFailure(
                "invalid_cose_encrypt_ciphertext",
                "A detached COSE_Encrypt message must not contain attached ciphertext bytes.");
        }

        if (message.Recipients == null
            || message.Recipients.Length == 0)
        {
            return ValidationFailure(
                "invalid_cose_encrypt_recipients",
                "A COSE_Encrypt message requires at least one recipient.");
        }

        int index = 0;
        HashSet<CoseRecipient> ancestors =
            new HashSet<CoseRecipient>();
        while (index < message.Recipients.Length)
        {
            TokenResult recipient = ValidateRecipient(
                message.Recipients[index],
                ancestors);
            if (!recipient.Success)
            {
                return recipient;
            }

            index += 1;
        }

        return ValidationSuccess();
    }

    internal static TokenResult ValidateRecipient(
        CoseRecipient recipient,
        HashSet<CoseRecipient> ancestors)
    {
        if (recipient == null || recipient.Header == null)
        {
            return ValidationFailure(
                "invalid_cose_recipient",
                "Each COSE recipient requires headers.");
        }

        if (!ancestors.Add(recipient))
        {
            return ValidationFailure(
                "cyclic_cose_recipient",
                "A COSE recipient cannot contain itself recursively.");
        }

        if (!recipient.EncryptedKeyAbsent
            && recipient.EncryptedKey == null)
        {
            return ValidationFailure(
                "invalid_cose_recipient_encrypted_key",
                "A present COSE recipient encrypted key requires bytes.");
        }

        if (recipient.EncryptedKeyAbsent
            && recipient.EncryptedKey != null)
        {
            return ValidationFailure(
                "invalid_cose_recipient_encrypted_key",
                "An absent COSE recipient encrypted key must not contain bytes.");
        }

        if (recipient.Recipients == null)
        {
            return ValidationFailure(
                "invalid_cose_recipient_children",
                "The nested COSE recipient collection is missing.");
        }

        int index = 0;
        while (index < recipient.Recipients.Length)
        {
            TokenResult child = ValidateRecipient(
                recipient.Recipients[index],
                ancestors);
            if (!child.Success)
            {
                ancestors.Remove(recipient);
                return child;
            }

            index += 1;
        }

        ancestors.Remove(recipient);
        return ValidationSuccess();
    }

    internal static TokenResult ValidatePolicy(TokenPolicy policy)
    {
        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumCborDepth <= 0)
        {
            return ValidationFailure(
                "invalid_policy",
                "The COSE_Encrypt policy contains an invalid size or depth limit.");
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

    internal static CoseEncryptResult Failure(TokenError? error)
    {
        CoseEncryptResult result = new CoseEncryptResult();
        result.Error = error;
        return result;
    }

    internal static TokenBytesResult BytesFailure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }

    internal static CoseRecipientResult RecipientFailure(TokenError? error)
    {
        CoseRecipientResult result = new CoseRecipientResult();
        result.Error = error;
        return result;
    }

    internal static CoseRecipientResult RecipientFailure(
        string code,
        string message,
        int position)
    {
        return RecipientFailure(new TokenError(code, message, position));
    }

    internal static CoseRecipientEncoding RecipientEncodingFailure(
        TokenError? error)
    {
        CoseRecipientEncoding result = new CoseRecipientEncoding();
        result.Error = error;
        return result;
    }
}
