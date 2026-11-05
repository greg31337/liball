using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>
/// Contains the outcome of JWE ECDH-ES key management.
/// </summary>
public class EcdhKeyManagementResult
{
    /// <summary>Indicates whether ECDH-ES key agreement succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when the operation succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the derived, generated, or caller-supplied JWE content-encryption key.</summary>
    public byte[]? ContentEncryptionKey;

    /// <summary>Contains the JWE encrypted-key value, which is empty for direct agreement.</summary>
    public byte[]? EncryptedKey;

    /// <summary>Contains the generated public ephemeral key for the JWE <c>epk</c> header.</summary>
    public TokenKey? EphemeralPublicKey;

    /// <summary>Initializes an unsuccessful result with no output or error details.</summary>
    public EcdhKeyManagementResult()
    {
        Success = false;
        Error = null;
        ContentEncryptionKey = null;
        EncryptedKey = null;
        EphemeralPublicKey = null;
    }
}
