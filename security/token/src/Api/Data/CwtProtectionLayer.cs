using System;
namespace token;

/// <summary>Describes one signed, MACed, or encrypted layer used to create a CWT.</summary>
/// <remarks>
/// For Sign1, Mac0, and Encrypt0, <see cref="Keys"/> contains one key and
/// <see cref="ParticipantHeaders"/> is empty. For Sign, Mac, and Encrypt, participant headers
/// and keys correspond by index. Static ECDH sender keys are optional for Mac and Encrypt.
/// </remarks>
public class CwtProtectionLayer
{
    /// <summary>Identifies the COSE message kind for this layer.</summary>
    public CwtProtectionKind Kind;

    /// <summary>Contains the message-level COSE header.</summary>
    public CoseHeader Header;

    /// <summary>Contains signer or recipient headers in participant order.</summary>
    public CoseHeader[] ParticipantHeaders;

    /// <summary>Contains signing, MAC, or recipient keys in participant order.</summary>
    public TokenKey[] Keys;

    /// <summary>Contains optional static ECDH sender private keys in recipient order.</summary>
    public TokenKey[]? SenderKeys;

    /// <summary>Contains application-supplied external authenticated data.</summary>
    public byte[] ExternalAuthenticatedData;

    /// <summary>Allows deprecated polymorphic ECDSA labels for signature layers.</summary>
    public bool AllowCompatibility;

    /// <summary>Initializes an empty Sign1 layer model.</summary>
    public CwtProtectionLayer()
    {
        Kind = CwtProtectionKind.Sign1;
        Header = new CoseHeader();
        ParticipantHeaders = Array.Empty<CoseHeader>();
        Keys = Array.Empty<TokenKey>();
        SenderKeys = null;
        ExternalAuthenticatedData = Array.Empty<byte>();
        AllowCompatibility = false;
    }

    /// <summary>Creates a COSE_Sign1 layer description.</summary>
    /// <param name="header">The message header with a protected signature algorithm.</param>
    /// <param name="key">The private signing key.</param>
    /// <returns>A configured layer.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CwtProtectionLayer CreateSign1(
        CoseHeader header,
        TokenKey key)
    {
        return CreateSingleLayer(CwtProtectionKind.Sign1, header, key);
    }

    /// <summary>Creates a COSE_Sign layer description.</summary>
    /// <param name="header">The shared message header.</param>
    /// <param name="signerHeaders">The signer headers.</param>
    /// <param name="keys">The corresponding private signing keys.</param>
    /// <returns>A configured layer.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CwtProtectionLayer CreateSign(
        CoseHeader header,
        CoseHeader[] signerHeaders,
        TokenKey[] keys)
    {
        return Multiple(
            CwtProtectionKind.Sign,
            header,
            signerHeaders,
            keys,
            null);
    }

    /// <summary>Creates a COSE_Mac0 layer description.</summary>
    /// <param name="header">The message header with a protected MAC algorithm.</param>
    /// <param name="key">The symmetric MAC key.</param>
    /// <returns>A configured layer.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CwtProtectionLayer CreateMac0(
        CoseHeader header,
        TokenKey key)
    {
        return CreateSingleLayer(CwtProtectionKind.Mac0, header, key);
    }

    /// <summary>Creates a COSE_Mac layer description.</summary>
    /// <param name="header">The shared MAC header.</param>
    /// <param name="recipientHeaders">The recipient headers.</param>
    /// <param name="keys">The corresponding recipient keys.</param>
    /// <param name="senderKeys">Optional static ECDH sender keys, or <c>null</c>.</param>
    /// <returns>A configured layer.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static CwtProtectionLayer CreateMac(
        CoseHeader header,
        CoseHeader[] recipientHeaders,
        TokenKey[] keys,
        TokenKey[]? senderKeys)
    {
        return Multiple(
            CwtProtectionKind.Mac,
            header,
            recipientHeaders,
            keys,
            senderKeys);
    }

    /// <summary>Creates a COSE_Encrypt0 layer description.</summary>
    /// <param name="header">The message header with a protected content-encryption algorithm.</param>
    /// <param name="key">The symmetric content-encryption key.</param>
    /// <returns>A configured layer.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CwtProtectionLayer CreateEncrypt0(
        CoseHeader header,
        TokenKey key)
    {
        return CreateSingleLayer(CwtProtectionKind.Encrypt0, header, key);
    }

    /// <summary>Creates a COSE_Encrypt layer description.</summary>
    /// <param name="header">The shared content-encryption header.</param>
    /// <param name="recipientHeaders">The recipient headers.</param>
    /// <param name="keys">The corresponding recipient keys.</param>
    /// <param name="senderKeys">Optional static ECDH sender keys, or <c>null</c>.</param>
    /// <returns>A configured layer.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static CwtProtectionLayer CreateEncrypt(
        CoseHeader header,
        CoseHeader[] recipientHeaders,
        TokenKey[] keys,
        TokenKey[]? senderKeys)
    {
        return Multiple(
            CwtProtectionKind.Encrypt,
            header,
            recipientHeaders,
            keys,
            senderKeys);
    }

    internal static CwtProtectionLayer CreateSingleLayer(
        CwtProtectionKind kind,
        CoseHeader header,
        TokenKey key)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        CwtProtectionLayer result = new CwtProtectionLayer();
        result.Kind = kind;
        result.Header = header;
        result.Keys = new TokenKey[] { key };
        return result;
    }

    internal static CwtProtectionLayer Multiple(
        CwtProtectionKind kind,
        CoseHeader header,
        CoseHeader[] participantHeaders,
        TokenKey[] keys,
        TokenKey[]? senderKeys)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        if (participantHeaders == null)
        {
            throw new ArgumentNullException("participantHeaders");
        }

        if (keys == null)
        {
            throw new ArgumentNullException("keys");
        }

        CwtProtectionLayer result = new CwtProtectionLayer();
        result.Kind = kind;
        result.Header = header;
        result.ParticipantHeaders = participantHeaders.AsSpan().ToArray();
        result.Keys = keys.AsSpan().ToArray();
        result.SenderKeys = senderKeys == null
            ? null
            : senderKeys.AsSpan().ToArray();
        return result;
    }
}
