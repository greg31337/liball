using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>
/// Contains the outcome of JWE AES-GCM content encryption.
/// </summary>
public class AesGcmContentEncryptionResult
{
    /// <summary>Indicates whether AES-GCM content encryption succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when encryption succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the JWE ciphertext.</summary>
    public byte[]? Ciphertext;

    /// <summary>Contains the 96-bit JWE initialization vector.</summary>
    public byte[]? InitializationVector;

    /// <summary>Contains the 128-bit JWE authentication tag.</summary>
    public byte[]? AuthenticationTag;

    /// <summary>Initializes an unsuccessful result with no output or error details.</summary>
    public AesGcmContentEncryptionResult()
    {
        Success = false;
        Error = null;
        Ciphertext = null;
        InitializationVector = null;
        AuthenticationTag = null;
    }
}
