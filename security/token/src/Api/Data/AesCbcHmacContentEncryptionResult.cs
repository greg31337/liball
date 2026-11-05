using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>
/// Contains the outcome of JWE AES-CBC-HMAC-SHA2 content encryption.
/// </summary>
public class AesCbcHmacContentEncryptionResult
{
    /// <summary>Indicates whether authenticated content encryption succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when encryption succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the JWE ciphertext.</summary>
    public byte[]? Ciphertext;

    /// <summary>Contains the 128-bit JWE initialization vector.</summary>
    public byte[]? InitializationVector;

    /// <summary>Contains the truncated JWE authentication tag.</summary>
    public byte[]? AuthenticationTag;

    /// <summary>Initializes an unsuccessful result with no output or error details.</summary>
    public AesCbcHmacContentEncryptionResult()
    {
        Success = false;
        Error = null;
        Ciphertext = null;
        InitializationVector = null;
        AuthenticationTag = null;
    }
}
