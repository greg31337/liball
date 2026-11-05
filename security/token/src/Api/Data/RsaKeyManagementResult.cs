using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>
/// Contains the outcome of RSA JWE key management.
/// </summary>
public class RsaKeyManagementResult
{
    /// <summary>Indicates whether RSA key management succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when the operation succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the content-encryption key.</summary>
    public byte[]? ContentEncryptionKey;

    /// <summary>Contains the RSA-encrypted content-encryption key.</summary>
    public byte[]? EncryptedKey;

    /// <summary>Initializes an unsuccessful RSA key-management result.</summary>
    public RsaKeyManagementResult()
    {
        Success = false;
        Error = null;
        ContentEncryptionKey = null;
        EncryptedKey = null;
    }
}
