using System;
namespace token;

/// <summary>Represents an RFC 9052 COSE recipient information structure.</summary>
public class CoseRecipient
{
    /// <summary>Contains the protected and unprotected recipient headers.</summary>
    public CoseHeader? Header;

    /// <summary>Contains the encrypted key, or <c>null</c> when the field is encoded as CBOR null.</summary>
    public byte[]? EncryptedKey;

    /// <summary>Indicates whether the encrypted-key field is encoded as CBOR null.</summary>
    public bool EncryptedKeyAbsent;

    /// <summary>Contains nested recipient information in serialization order.</summary>
    public CoseRecipient[] Recipients;

    /// <summary>Indicates whether this recipient successfully recovered the content key.</summary>
    public bool Decrypted;

    /// <summary>Initializes an empty recipient with no nested recipients.</summary>
    public CoseRecipient()
    {
        Header = new CoseHeader();
        EncryptedKey = Array.Empty<byte>();
        EncryptedKeyAbsent = false;
        Recipients = Array.Empty<CoseRecipient>();
        Decrypted = false;
    }
}
