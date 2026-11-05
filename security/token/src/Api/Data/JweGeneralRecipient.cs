using System;
using System.Security.Cryptography;
using System.Text;

namespace token;

/// <summary>Represents one recipient entry in a general JWE JSON serialization.</summary>
public class JweGeneralRecipient
{
    /// <summary>Contains the recipient-specific unprotected header, or <c>null</c>.</summary>
    public JoseHeader? Header;

    /// <summary>Contains the JWE key-management algorithm identifier.</summary>
    public string? Algorithm;

    /// <summary>Contains registry metadata for <see cref="Algorithm"/>.</summary>
    public AlgorithmMetadata? AlgorithmMetadata;

    /// <summary>Indicates whether the key-management algorithm has a usable implementation.</summary>
    public bool AlgorithmAvailable;

    /// <summary>Contains the key identifier from the combined recipient header, or <c>null</c>.</summary>
    public string? KeyId;

    /// <summary>Contains the decoded encrypted content-encryption key.</summary>
    public byte[]? EncryptedKey;

    /// <summary>Indicates whether this recipient successfully unwrapped the authenticated content.</summary>
    public bool Decrypted;

    internal string? FlattenedJson;

    /// <summary>Initializes an empty, undecrypted recipient entry.</summary>
    public JweGeneralRecipient()
    {
        Header = null;
        Algorithm = null;
        AlgorithmMetadata = null;
        AlgorithmAvailable = false;
        KeyId = null;
        EncryptedKey = null;
        Decrypted = false;
        FlattenedJson = null;
    }
}
