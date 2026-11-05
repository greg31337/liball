using System;
using System.Security.Cryptography;

namespace token;

/// <summary>Represents a parsed compact JWE without implying authentication or decryption.</summary>
public class JweCompactToken
{
    /// <summary>Identifies this representation as compact JWE.</summary>
    public TokenKind Kind;

    /// <summary>Contains the integrity-protected JOSE header.</summary>
    public JoseHeader? ProtectedHeader;

    /// <summary>Contains the JWE key-management algorithm identifier.</summary>
    public string? Algorithm;

    /// <summary>Contains registry metadata for <see cref="Algorithm"/>.</summary>
    public AlgorithmMetadata? AlgorithmMetadata;

    /// <summary>Indicates whether the key-management algorithm has a usable built-in implementation.</summary>
    public bool AlgorithmAvailable;

    /// <summary>Contains the JWE content-encryption algorithm identifier.</summary>
    public string? EncryptionAlgorithm;

    /// <summary>Contains registry metadata for <see cref="EncryptionAlgorithm"/>.</summary>
    public AlgorithmMetadata? EncryptionAlgorithmMetadata;

    /// <summary>Indicates whether the content-encryption algorithm has a usable built-in implementation.</summary>
    public bool EncryptionAlgorithmAvailable;

    /// <summary>Contains the compression algorithm identifier, or <c>null</c> when absent.</summary>
    public string? CompressionAlgorithm;

    /// <summary>Indicates whether <see cref="CompressionAlgorithm"/> is registered.</summary>
    public bool CompressionAlgorithmKnown;

    /// <summary>Indicates whether the compression algorithm has a built-in implementation.</summary>
    public bool CompressionAlgorithmAvailable;

    /// <summary>Contains the key identifier, or <c>null</c> when absent.</summary>
    public string? KeyId;

    /// <summary>Contains the decoded encrypted content-encryption key.</summary>
    public byte[]? EncryptedKey;

    /// <summary>Contains the decoded content-encryption initialization vector.</summary>
    public byte[]? InitializationVector;

    /// <summary>Contains ciphertext bytes after a policy-authorized diagnostic read.</summary>
    public byte[]? Ciphertext;

    /// <summary>Indicates whether <see cref="Ciphertext"/> is available.</summary>
    public bool CiphertextAvailable;

    /// <summary>Contains the decoded content-encryption authentication tag.</summary>
    public byte[]? AuthenticationTag;

    /// <summary>Contains plaintext after successful decryption.</summary>
    public byte[]? Plaintext;

    /// <summary>Indicates whether <see cref="Plaintext"/> is available.</summary>
    public bool PlaintextAvailable;

    /// <summary>Indicates whether authenticated decryption has succeeded.</summary>
    public bool AuthenticationTagVerified;

    internal string? ProtectedSegment;
    internal byte[]? ParsedCiphertext;

    /// <summary>Initializes an empty, unauthenticated compact JWE model.</summary>
    public JweCompactToken()
    {
        Kind = TokenKind.JweCompact;
        ProtectedHeader = null;
        Algorithm = null;
        AlgorithmMetadata = null;
        AlgorithmAvailable = false;
        EncryptionAlgorithm = null;
        EncryptionAlgorithmMetadata = null;
        EncryptionAlgorithmAvailable = false;
        CompressionAlgorithm = null;
        CompressionAlgorithmKnown = false;
        CompressionAlgorithmAvailable = false;
        KeyId = null;
        EncryptedKey = null;
        InitializationVector = null;
        Ciphertext = null;
        CiphertextAvailable = false;
        AuthenticationTag = null;
        Plaintext = null;
        PlaintextAvailable = false;
        AuthenticationTagVerified = false;
        ProtectedSegment = null;
        ParsedCiphertext = null;
    }
}
