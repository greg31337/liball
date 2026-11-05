using System;
namespace token;

/// <summary>Describes the byte-string fields of an RFC 9338 countersignature target.</summary>
public class CoseCounterSignatureTarget
{
    /// <summary>Contains the exact serialized protected header of the target.</summary>
    public byte[] BodyProtected;

    /// <summary>Contains the target payload, ciphertext, or signature bytes.</summary>
    public byte[] Payload;

    /// <summary>Contains additional target byte strings after the payload.</summary>
    public byte[][] OtherFields;

    /// <summary>Initializes an empty target.</summary>
    public CoseCounterSignatureTarget()
    {
        BodyProtected = Array.Empty<byte>();
        Payload = Array.Empty<byte>();
        OtherFields = Array.Empty<byte[]>();
    }

    /// <summary>Creates a target for a finalized COSE_Sign1 message.</summary>
    /// <param name="message">The signed message.</param>
    /// <param name="payload">The attached or separately transported payload.</param>
    /// <returns>A target that also binds the primary signature.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseCounterSignatureTarget FromSign1(
        CoseSign1 message,
        byte[] payload)
    {
        ValidateMessage(message, payload, message == null ? null : message.Signature);
        ValidateTargetHeader((message ?? throw new InvalidOperationException("A required value is null.")).Header);
        return CreateTarget(
            (message.Header ?? throw new InvalidOperationException("A required value is null.")).ProtectedBytes,
            payload,
            new byte[][] { (message.Signature ?? throw new InvalidOperationException("A required value is null.")) });
    }

    /// <summary>Creates a target for a finalized COSE_Sign message body.</summary>
    /// <param name="message">The signed message.</param>
    /// <param name="payload">The attached or separately transported payload.</param>
    /// <returns>A target for the common signed body.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseCounterSignatureTarget FromSign(
        CoseSign message,
        byte[] payload)
    {
        ValidateMessage(message, payload, Array.Empty<byte>());
        ValidateTargetHeader(message.Header);
        return CreateTarget(
            (message.Header ?? throw new InvalidOperationException("A required value is null.")).ProtectedBytes,
            payload,
            Array.Empty<byte[]>());
    }

    /// <summary>Creates a target for one finalized COSE_Signature entry.</summary>
    /// <param name="signature">The primary signature entry.</param>
    /// <param name="payload">The payload signed by the primary signature.</param>
    /// <returns>A target for the signature entry.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseCounterSignatureTarget FromSignature(
        CoseSignature signature,
        byte[] payload)
    {
        if (signature == null)
        {
            throw new ArgumentNullException("signature");
        }

        ValidateMessage(signature.Header, payload, signature.Signature);
        ValidateTargetHeader(signature.Header);
        return CreateTarget(
            (signature.Header ?? throw new InvalidOperationException("A required value is null.")).ProtectedBytes,
            payload,
            Array.Empty<byte[]>());
    }

    /// <summary>Creates a target for a finalized COSE_Mac0 message.</summary>
    /// <param name="message">The MACed message.</param>
    /// <param name="payload">The attached or separately transported payload.</param>
    /// <returns>A target that also binds the authentication tag.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseCounterSignatureTarget FromMac0(
        CoseMac0 message,
        byte[] payload)
    {
        ValidateMessage(message, payload, message == null ? null : message.Tag);
        ValidateTargetHeader((message ?? throw new InvalidOperationException("A required value is null.")).Header);
        return CreateTarget(
            (message.Header ?? throw new InvalidOperationException("A required value is null.")).ProtectedBytes,
            payload,
            new byte[][] { (message.Tag ?? throw new InvalidOperationException("A required value is null.")) });
    }

    /// <summary>Creates a target for a finalized COSE_Mac message.</summary>
    /// <param name="message">The MACed message.</param>
    /// <param name="payload">The attached or separately transported payload.</param>
    /// <returns>A target that also binds the authentication tag.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseCounterSignatureTarget FromMac(
        CoseMac message,
        byte[] payload)
    {
        ValidateMessage(message, payload, message == null ? null : message.Tag);
        ValidateTargetHeader((message ?? throw new InvalidOperationException("A required value is null.")).Header);
        return CreateTarget(
            (message.Header ?? throw new InvalidOperationException("A required value is null.")).ProtectedBytes,
            payload,
            new byte[][] { (message.Tag ?? throw new InvalidOperationException("A required value is null.")) });
    }

    /// <summary>Creates a target for a finalized COSE_Encrypt0 message.</summary>
    /// <param name="message">The encrypted message.</param>
    /// <param name="ciphertext">The attached or separately transported ciphertext.</param>
    /// <returns>A target that attests to the encrypted bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseCounterSignatureTarget FromEncrypt0(
        CoseEncrypt0 message,
        byte[] ciphertext)
    {
        ValidateMessage(message, ciphertext, Array.Empty<byte>());
        ValidateTargetHeader(message.Header);
        return CreateTarget(
            (message.Header ?? throw new InvalidOperationException("A required value is null.")).ProtectedBytes,
            ciphertext,
            Array.Empty<byte[]>());
    }

    /// <summary>Creates a target for a finalized COSE_Encrypt message.</summary>
    /// <param name="message">The encrypted message.</param>
    /// <param name="ciphertext">The attached or separately transported ciphertext.</param>
    /// <returns>A target that attests to the encrypted bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseCounterSignatureTarget FromEncrypt(
        CoseEncrypt message,
        byte[] ciphertext)
    {
        ValidateMessage(message, ciphertext, Array.Empty<byte>());
        ValidateTargetHeader(message.Header);
        return CreateTarget(
            (message.Header ?? throw new InvalidOperationException("A required value is null.")).ProtectedBytes,
            ciphertext,
            Array.Empty<byte[]>());
    }

    /// <summary>Creates a target for a finalized COSE recipient.</summary>
    /// <param name="recipient">The recipient information.</param>
    /// <param name="encryptedKey">The recipient encrypted-key bytes.</param>
    /// <returns>A target for the recipient information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseCounterSignatureTarget FromRecipient(
        CoseRecipient recipient,
        byte[] encryptedKey)
    {
        if (recipient == null)
        {
            throw new ArgumentNullException("recipient");
        }

        ValidateMessage(recipient.Header, encryptedKey, Array.Empty<byte>());
        ValidateTargetHeader(recipient.Header);
        return CreateTarget(
            (recipient.Header ?? throw new InvalidOperationException("A required value is null.")).ProtectedBytes,
            encryptedKey,
            Array.Empty<byte[]>());
    }

    internal static CoseCounterSignatureTarget CreateTarget(
        byte[] bodyProtected,
        byte[] payload,
        byte[][] otherFields)
    {
        CoseCounterSignatureTarget target =
            new CoseCounterSignatureTarget();
        target.BodyProtected = Copy(bodyProtected);
        target.Payload = Copy(payload);
        target.OtherFields = new byte[otherFields.Length][];
        int index = 0;
        while (index < otherFields.Length)
        {
            target.OtherFields[index] = Copy(otherFields[index]);
            index += 1;
        }

        return target;
    }

    internal static void ValidateMessage(
        object? message,
        byte[] payload,
        byte[]? cryptographicValue)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (payload == null)
        {
            throw new ArgumentNullException("payload");
        }

        if (cryptographicValue == null)
        {
            throw new ArgumentNullException("cryptographicValue");
        }
    }

    internal static void ValidateTargetHeader(CoseHeader? header)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        if (header.ProtectedBytes == null)
        {
            throw new ArgumentNullException("protectedBytes");
        }
    }

    internal static byte[] Copy(byte[] value)
    {
        return value.AsSpan().ToArray();
    }
}
