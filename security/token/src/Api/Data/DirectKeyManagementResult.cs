using System;
namespace token;

/// <summary>
/// Contains the outcome of JWE direct key management.
/// </summary>
public class DirectKeyManagementResult
{
    /// <summary>Indicates whether direct key management succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when the operation succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains an independent copy of the content-encryption key.</summary>
    public byte[]? ContentEncryptionKey;

    /// <summary>Contains the JWE encrypted-key value, which is empty for direct key management.</summary>
    public byte[]? EncryptedKey;

    /// <summary>Initializes an unsuccessful direct key-management result.</summary>
    public DirectKeyManagementResult()
    {
        Success = false;
        Error = null;
        ContentEncryptionKey = null;
        EncryptedKey = null;
    }
}
