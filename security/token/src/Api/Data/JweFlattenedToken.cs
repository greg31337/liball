using System;
using System.Security.Cryptography;
using System.Text;

namespace token;

/// <summary>Represents one flattened JWE JSON serialization.</summary>
public class JweFlattenedToken
{
    /// <summary>Identifies this representation as JWE JSON.</summary>
    public TokenKind Kind;

    /// <summary>Contains the integrity-protected JOSE header.</summary>
    public JoseHeader? ProtectedHeader;

    /// <summary>Contains the shared unprotected JOSE header, or <c>null</c>.</summary>
    public JoseHeader? SharedUnprotectedHeader;

    /// <summary>Contains the recipient JOSE header, or <c>null</c>.</summary>
    public JoseHeader? RecipientHeader;

    /// <summary>Contains the JWE key-management algorithm identifier.</summary>
    public string? Algorithm;

    /// <summary>Contains registry metadata for <see cref="Algorithm"/>.</summary>
    public AlgorithmMetadata? AlgorithmMetadata;

    /// <summary>Indicates whether the key-management algorithm has a usable implementation.</summary>
    public bool AlgorithmAvailable;

    /// <summary>Contains the JWE content-encryption algorithm identifier.</summary>
    public string? EncryptionAlgorithm;

    /// <summary>Contains registry metadata for <see cref="EncryptionAlgorithm"/>.</summary>
    public AlgorithmMetadata? EncryptionAlgorithmMetadata;

    /// <summary>Indicates whether the content-encryption algorithm has a usable implementation.</summary>
    public bool EncryptionAlgorithmAvailable;

    /// <summary>Contains the compression algorithm identifier, or <c>null</c>.</summary>
    public string? CompressionAlgorithm;

    /// <summary>Indicates whether <see cref="CompressionAlgorithm"/> is registered.</summary>
    public bool CompressionAlgorithmKnown;

    /// <summary>Indicates whether the compression algorithm has a usable implementation.</summary>
    public bool CompressionAlgorithmAvailable;

    /// <summary>Contains the key identifier from the combined JOSE header, or <c>null</c>.</summary>
    public string? KeyId;

    /// <summary>Contains the decoded encrypted content-encryption key.</summary>
    public byte[]? EncryptedKey;

    /// <summary>Contains the decoded content-encryption initialization vector.</summary>
    public byte[]? InitializationVector;

    /// <summary>Contains ciphertext after a policy-authorized diagnostic read.</summary>
    public byte[]? Ciphertext;

    /// <summary>Indicates whether <see cref="Ciphertext"/> is available.</summary>
    public bool CiphertextAvailable;

    /// <summary>Contains the decoded content-encryption authentication tag.</summary>
    public byte[]? AuthenticationTag;

    /// <summary>Contains decoded external Additional Authenticated Data, or <c>null</c> when absent.</summary>
    public byte[]? AdditionalAuthenticatedData;

    /// <summary>Indicates whether the JWE JSON serialization contains an <c>aad</c> member.</summary>
    public bool AdditionalAuthenticatedDataPresent;

    /// <summary>Contains plaintext after successful authenticated decryption.</summary>
    public byte[]? Plaintext;

    /// <summary>Indicates whether <see cref="Plaintext"/> is available.</summary>
    public bool PlaintextAvailable;

    /// <summary>Indicates whether authenticated decryption succeeded.</summary>
    public bool AuthenticationTagVerified;

    internal JoseHeader? EffectiveHeader;
    internal string? ProtectedSegment;
    internal string? AdditionalAuthenticatedDataSegment;
    internal byte[]? ParsedCiphertext;

    /// <summary>Initializes an empty, unauthenticated flattened JWE model.</summary>
    public JweFlattenedToken()
    {
        Kind = TokenKind.JweJson;
        ProtectedHeader = null;
        SharedUnprotectedHeader = null;
        RecipientHeader = null;
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
        AdditionalAuthenticatedData = null;
        AdditionalAuthenticatedDataPresent = false;
        Plaintext = null;
        PlaintextAvailable = false;
        AuthenticationTagVerified = false;
        EffectiveHeader = null;
        ProtectedSegment = null;
        AdditionalAuthenticatedDataSegment = null;
        ParsedCiphertext = null;
    }
}
