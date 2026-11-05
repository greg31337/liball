using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace token;

/// <summary>
/// Contains the outcome of JWE AES-GCM key wrapping.
/// </summary>
public class AesGcmKeyWrapResult
{
    /// <summary>Indicates whether AES-GCM key wrapping succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when the operation succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the JWE encrypted-key value.</summary>
    public byte[]? EncryptedKey;

    /// <summary>Contains the 96-bit initialization vector for the JWE <c>iv</c> header.</summary>
    public byte[]? InitializationVector;

    /// <summary>Contains the 128-bit authentication tag for the JWE <c>tag</c> header.</summary>
    public byte[]? AuthenticationTag;

    /// <summary>Initializes an unsuccessful result with no output or error details.</summary>
    public AesGcmKeyWrapResult()
    {
        Success = false;
        Error = null;
        EncryptedKey = null;
        InitializationVector = null;
        AuthenticationTag = null;
    }
}
